// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_a_reactor_has_been_unseen_longer_than_the_period.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// A client reactor whose client disconnected and stayed away longer than the period is removed, with everything the kernel held
/// for it, and no longer shows in the listing of observers.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_a_reactor_has_been_unseen_longer_than_the_period(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public given.Footprint Before = given.Footprint.None;
        public given.Footprint After = given.Footprint.None;
        public int RemovedBySweep;
        public int RemovedBySecondSweep;

        async Task Because()
        {
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await _store.EventLog.Append("source", new given.UnseenEvent("first"));
            await _store.EventLog.Append("other", new given.UnseenEvent("second"));
            await WaitForHandled(alpha, 2);
            Before = await FootprintOf(ObserverId);

            await GoAway(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            RemovedBySweep = await Sweeper.Sweep();
            After = await FootprintOf(ObserverId);
            RemovedBySecondSweep = await Sweeper.Sweep();
        }
    }

    [Fact] void should_have_recorded_the_durable_subscription_before() => Context.Before.HasDurableSubscription.ShouldBeTrue();
    [Fact] void should_have_had_a_reminder_before() => Context.Before.Reminders.ShouldBeGreaterThan(0);
    [Fact] void should_have_had_a_definition_and_a_state_before() => (Context.Before.HasDefinition && Context.Before.HasState).ShouldBeTrue();
    [Fact] void should_have_listed_the_observer_before() => Context.Before.IsListed.ShouldBeTrue();
    [Fact] void should_remove_exactly_that_observer() => Context.RemovedBySweep.ShouldEqual(1);
    [Fact] void should_remove_the_definition() => Context.After.HasDefinition.ShouldBeFalse();
    [Fact] void should_remove_the_state() => Context.After.HasState.ShouldBeFalse();
    [Fact] void should_leave_no_failed_partitions() => Context.After.FailedPartitions.ShouldEqual(0);
    [Fact] void should_leave_no_jobs() => Context.After.Jobs.ShouldEqual(0);
    [Fact] void should_leave_no_reminders() => Context.After.Reminders.ShouldEqual(0);
    [Fact] void should_leave_no_durable_subscription_record() => Context.After.HasDurableSubscription.ShouldBeFalse();
    [Fact] void should_no_longer_list_the_observer() => Context.After.IsListed.ShouldBeFalse();
    [Fact] void should_find_nothing_to_remove_when_swept_again() => Context.RemovedBySecondSweep.ShouldEqual(0);
}
