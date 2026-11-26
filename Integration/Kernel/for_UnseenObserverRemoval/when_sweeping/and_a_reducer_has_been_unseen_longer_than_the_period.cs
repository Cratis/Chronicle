// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_a_reducer_has_been_unseen_longer_than_the_period.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// The same for a client reducer: it is removed with its records, and the read model it built is still there afterwards.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_a_reducer_has_been_unseen_longer_than_the_period(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public given.Footprint Before = given.Footprint.None;
        public given.Footprint After = given.Footprint.None;
        public int RemovedBySweep;
        public given.UnseenReadModel? ReadModelBefore;
        public given.UnseenReadModel? ReadModelAfter;

        async Task Because()
        {
            var reducer = new given.UnseenReducer();
            var client = await StartClient(reducer);
            ObserverId = client.Reducer<given.UnseenReducer>().Id.Value;
            var appended = await Append(_store, 3);
            await WaitForHandledInOrder(reducer, appended);
            await WaitUntil("the read model to be built", async () => (await ReadModel("source"))?.Count == 3);
            ReadModelBefore = await ReadModel("source");
            Before = await FootprintOf(ObserverId);

            await GoAway(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            RemovedBySweep = await Sweeper.Sweep();
            After = await FootprintOf(ObserverId);
            ReadModelAfter = await ReadModel("source");
        }
    }

    [Fact] void should_have_had_a_definition_and_a_state_before() => (Context.Before.HasDefinition && Context.Before.HasState).ShouldBeTrue();
    [Fact] void should_have_built_the_read_model_before() => Context.ReadModelBefore!.Count.ShouldEqual(3);
    [Fact] void should_remove_exactly_that_observer() => Context.RemovedBySweep.ShouldEqual(1);
    [Fact] void should_remove_the_definition() => Context.After.HasDefinition.ShouldBeFalse();
    [Fact] void should_remove_the_state() => Context.After.HasState.ShouldBeFalse();
    [Fact] void should_leave_no_failed_partitions() => Context.After.FailedPartitions.ShouldEqual(0);
    [Fact] void should_leave_no_jobs() => Context.After.Jobs.ShouldEqual(0);
    [Fact] void should_leave_no_reminders() => Context.After.Reminders.ShouldEqual(0);
    [Fact] void should_leave_no_durable_subscription_record() => Context.After.HasDurableSubscription.ShouldBeFalse();
    [Fact] void should_no_longer_list_the_observer() => Context.After.IsListed.ShouldBeFalse();
    [Fact] void should_keep_the_read_model_data() => Context.ReadModelAfter.ShouldEqual(Context.ReadModelBefore);
}
