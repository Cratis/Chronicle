// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_the_safety_net_runs.and_one_observer_is_behind.context;
using KernelEventSequenceNumber = Cratis.Chronicle.Concepts.Events.EventSequenceNumber;
using KernelNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_the_safety_net_runs;

/// <summary>
/// The safety net of the appended-events queue wakes only the dormant observers that are behind (#6114). One observer has a
/// persisted cursor that is behind the tail of its event type, which is what a wake-up lost on its way to a dormant observer
/// leaves behind; the other has caught up and must neither be woken nor have its alerts reported (#6113). The behind observer
/// re-subscribes its client while it catches up, which starts a new alert lifecycle and reports it; that is not asserted here.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.DormantSafetyNetCollection.Name)]
public class and_one_observer_is_behind(context context) : Given<context>(context)
{
    public class context(given.DormantSafetyNetFixture fixture) : given.two_dormant_observers(fixture)
    {
        public int AlphaWakesBefore;
        public int BetaWakesBefore;
        public int BetaAlertCallsBefore;
        public int AlphaWakes;
        public int BetaWakes;
        public int BetaAlertCalls;
        public int AlphaEventHandledTimes;
        public EventSequenceNumber[] BetaHandled;

        async Task Because()
        {
            AlphaWakesBefore = SafetyNetWakes(_alpha);
            BetaWakesBefore = SafetyNetWakes(_beta);
            BetaAlertCallsBefore = AlertCalls(_beta);

            // Put the cursor of the alpha observer back to its event, as if the wake-up that followed the append had been lost.
            // Only the cursor changes: the rest of what the observer persisted is left as the observer wrote it.
            var observerStates = Services.GetRequiredService<IDatabase>()
                .GetEventStoreDatabase(_store.Name.Value)
                .GetNamespaceDatabase(KernelNamespaceName.Default)
                .GetObserverStateCollection();
            var rewound = await observerStates.UpdateOneAsync(
                _ => _.Id == _alpha.Id.Value,
                Builders<Storage.MongoDB.Observation.ObserverState>.Update.Set(_ => _.NextEventSequenceNumber, (KernelEventSequenceNumber)_alphaEvent.Value));
            rewound.MatchedCount.ShouldEqual(1);

            // The wake itself comes within seconds; the observer then has to take its subscription up again before it delivers.
            var deadline = DateTimeOffset.UtcNow.AddSeconds(150);
            while (Alpha.Handled.Count < 2)
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException($"The observer that was behind was not woken to deliver its event: {SafetyNetWakes(_alpha)} wakes");
                }

                await Task.Delay(250);
            }

            // The beta observer is looked at every few seconds too: stay long enough for it to have been looked at, and for the
            // alpha observer to settle and be released again.
            await Task.Delay(TimeSpan.FromSeconds(25));

            AlphaWakes = SafetyNetWakes(_alpha) - AlphaWakesBefore;
            BetaWakes = SafetyNetWakes(_beta) - BetaWakesBefore;
            BetaAlertCalls = AlertCalls(_beta) - BetaAlertCallsBefore;
            AlphaEventHandledTimes = Alpha.Handled.Count(_ => _ == _alphaEvent);
            BetaHandled = [.. Beta.Handled];
        }
    }

    [Fact] void should_wake_the_observer_that_is_behind() => Context.AlphaWakes.ShouldBeGreaterThan(0);
    [Fact] void should_deliver_the_event_the_observer_was_behind_on() => Context.AlphaEventHandledTimes.ShouldEqual(2);
    [Fact] void should_not_wake_the_observer_that_is_up_to_date() => Context.BetaWakes.ShouldEqual(0);
    [Fact] void should_not_deliver_anything_again_to_the_observer_that_is_up_to_date() => Context.BetaHandled.Length.ShouldEqual(1);
    [Fact] void should_not_report_alerts_for_the_observer_that_stayed_dormant() => Context.BetaAlertCalls.ShouldEqual(0);
}
