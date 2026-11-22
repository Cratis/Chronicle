// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_dormant_observer_is_woken.and_it_is_healthy.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_dormant_observer_is_woken;

/// <summary>
/// A healthy observer that is woken from dormancy leaves the alerts alone (#6113): it has no failed partition, no open incident
/// and no quarantine, so nothing about its alert state has changed. Waking it must not call the companion grain that reports
/// alerts, which is what reads the alert storage and drains the System event log, and it must not be woken to report either.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.DormantSafetyNetCollection.Name)]
public class and_it_is_healthy(context context) : Given<context>(context)
{
    public class context(given.DormantSafetyNetFixture fixture) : given.two_dormant_observers(fixture)
    {
        public int AlertCallsBefore;
        public int AlertCallsAfter;
        public int AlphaHandled;
        public bool ResidentAfterwards;

        async Task Because()
        {
            AlertCallsBefore = AlertCalls(_alpha);

            // Appended while the observer has no activation: the queue wakes it, it handles the event and goes dormant again.
            await _store.EventLog.Append("alpha", new given.DormantAlphaEvent("second"));
            await WaitForHandled(Alpha.Handled, 2);
            await WaitUntilReleased(_alpha);

            // Long enough for a report that was scheduled by the activation to have been made.
            await Task.Delay(TimeSpan.FromSeconds(10));
            AlertCallsAfter = AlertCalls(_alpha);
            AlphaHandled = Alpha.Handled.Count;
            ResidentAfterwards = (await ResidentObserverGrains(_alpha)).Any();
        }
    }

    [Fact] void should_have_woken_the_observer_to_handle_the_event() => Context.AlphaHandled.ShouldEqual(2);
    [Fact] void should_not_call_the_alert_companion_of_the_observer() => Context.AlertCallsAfter.ShouldEqual(Context.AlertCallsBefore);
    [Fact] void should_leave_the_observer_dormant_again() => Context.ResidentAfterwards.ShouldBeFalse();
}
