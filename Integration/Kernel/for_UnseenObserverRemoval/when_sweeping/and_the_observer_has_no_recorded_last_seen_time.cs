// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_observer_has_no_recorded_last_seen_time.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// An observer with no recorded time, as every observer is when a kernel that did not record it is upgraded, is not removed on the
/// first sweep. The sweep starts its clock, and it is removed only after the period from that point.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_observer_has_no_recorded_last_seen_time(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public const string ObserverId = "an-observer-from-before-the-upgrade";
        public int RemovedByTheFirstSweep;
        public int RemovedByAnImmediateSweep;
        public int RemovedAfterThePeriod;
        public DateTimeOffset FirstSweepStarted;
        public DateTimeOffset ClockStartedAt;
        public given.Footprint AfterTheFirstSweep = given.Footprint.None;
        public given.Footprint AfterThePeriod = given.Footprint.None;

        async Task Because()
        {
            await StoreStorage.Observers.Save(new ObserverDefinition(ObserverId, [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true));
            await States().Save(new ObserverState { Identifier = ObserverId, RunningState = ObserverRunningState.Disconnected }, EventSequenceId.Log);

            FirstSweepStarted = DateTimeOffset.UtcNow;
            RemovedByTheFirstSweep = await Sweeper.Sweep();
            ClockStartedAt = await LastSeen(ObserverId);
            AfterTheFirstSweep = await FootprintOf(ObserverId);
            RemovedByAnImmediateSweep = await Sweeper.Sweep();

            await WaitUntilUnseenForThePeriod(ObserverId);
            RemovedAfterThePeriod = await Sweeper.Sweep();
            AfterThePeriod = await FootprintOf(ObserverId);
        }
    }

    [Fact] void should_not_remove_it_on_the_first_sweep() => Context.RemovedByTheFirstSweep.ShouldEqual(0);
    [Fact] void should_keep_it_after_the_first_sweep() => (Context.AfterTheFirstSweep.HasDefinition && Context.AfterTheFirstSweep.HasState).ShouldBeTrue();
    [Fact] void should_start_its_clock_at_the_first_sweep() => Context.ClockStartedAt.ShouldBeGreaterThanOrEqual(Context.FirstSweepStarted);
    [Fact] void should_not_remove_it_by_sweeping_again_straight_away() => Context.RemovedByAnImmediateSweep.ShouldEqual(0);
    [Fact] void should_remove_it_once_the_period_has_passed_since_the_clock_started() => Context.RemovedAfterThePeriod.ShouldEqual(1);
    [Fact] void should_remove_the_definition() => Context.AfterThePeriod.HasDefinition.ShouldBeFalse();
    [Fact] void should_remove_the_state() => Context.AfterThePeriod.HasState.ShouldBeFalse();
}
