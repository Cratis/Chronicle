// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_observer_is_quarantined.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// An observer that is quarantined, because too many of its partitions failed, is not removed, however long its client has been away.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_observer_is_quarantined(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public int RemovedBySweep;
        public given.Footprint After = given.Footprint.None;
        public ObserverRunningState StateBeforeTheSweep;
        public ObserverRunningState StateAfterTheSweep;
        public DateTimeOffset GoneSince;
        public DateTimeOffset SweptAt;

        async Task Because()
        {
            var failing = new given.UnseenFailingReactor();
            var client = await StartClient(failing);
            ObserverId = client.Reactor<given.UnseenFailingReactor>().Id.Value;
            await _store.EventLog.Append("first", new given.UnseenEvent("this one fails"));
            await _store.EventLog.Append("second", new given.UnseenEvent("this one fails"));
            await _store.EventLog.Append("third", new given.UnseenEvent("this one fails"));
            await WaitUntil("the observer to be quarantined", async () => (await States().Get(ObserverId)).RunningState == ObserverRunningState.Quarantined);

            await GoAwayLeavingTheObserverAsItIs(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);
            GoneSince = await LastSeen(ObserverId);
            StateBeforeTheSweep = (await States().Get(ObserverId)).RunningState;
            SweptAt = DateTimeOffset.UtcNow;

            RemovedBySweep = await Sweeper.Sweep();
            After = await FootprintOf(ObserverId);
            StateAfterTheSweep = (await States().Get(ObserverId)).RunningState;
        }
    }

    [Fact] void should_have_been_unseen_for_longer_than_the_period() => (Context.SweptAt - Context.GoneSince).ShouldBeGreaterThan(given.UnseenObserverFixture.Period);
    [Fact] void should_have_been_quarantined_when_swept() => Context.StateBeforeTheSweep.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_remove_nothing() => Context.RemovedBySweep.ShouldEqual(0);
    [Fact] void should_still_be_quarantined() => Context.StateAfterTheSweep.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_definition() => Context.After.HasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_failed_partitions() => Context.After.FailedPartitions.ShouldEqual(3);
}
