// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns.and_the_reducer_was_removed.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns;

/// <summary>
/// The same for a reducer: it replays from the start and the read model it builds has every event handled once, as it did before.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_reducer_was_removed(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public EventSequenceNumber[] Appended = [];
        public EventSequenceNumber[] HandledTheFirstTime = [];
        public EventSequenceNumber[] HandledAfterTheReturn = [];
        public given.UnseenReadModel? ReadModelTheFirstTime;
        public given.UnseenReadModel? ReadModelAfterTheRemoval;
        public given.UnseenReadModel? ReadModelAfterTheReturn;
        public given.Footprint AfterTheReturn = given.Footprint.None;
        public int Removed;

        async Task Because()
        {
            var first = new given.UnseenReducer();
            var client = await StartClient(first);
            ObserverId = client.Reducer<given.UnseenReducer>().Id.Value;
            Appended = await Append(_store, 3);
            await WaitForHandledInOrder(first, Appended);
            await WaitUntil("the read model to be built", async () => (await ReadModel("source"))?.Count == 3);
            HandledTheFirstTime = [.. first.Handled];
            ReadModelTheFirstTime = await ReadModel("source");

            await GoAway(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);
            Removed = await Sweeper.Sweep();
            ReadModelAfterTheRemoval = await ReadModel("source");

            var again = new given.UnseenReducer();
            await StartClient(again);
            await WaitForHandledInOrder(again, Appended);
            HandledAfterTheReturn = [.. again.Handled];
            await WaitUntil("the read model to be rebuilt", async () => (await ReadModel("source"))?.Count >= 3);
            ReadModelAfterTheReturn = await ReadModel("source");
            AfterTheReturn = await FootprintOf(ObserverId);
        }
    }

    [Fact] void should_have_removed_the_observer() => Context.Removed.ShouldEqual(1);
    [Fact] void should_have_kept_the_read_model_through_the_removal() => Context.ReadModelAfterTheRemoval.ShouldEqual(Context.ReadModelTheFirstTime);
    [Fact] void should_handle_every_earlier_event_again_in_order() => Context.HandledAfterTheReturn.ShouldEqual(Context.HandledTheFirstTime);
    [Fact] void should_register_the_observer_as_new() => (Context.AfterTheReturn.HasDefinition && Context.AfterTheReturn.HasState && Context.AfterTheReturn.IsListed).ShouldBeTrue();
    [Fact] void should_have_rebuilt_the_read_model_from_the_start() => Context.ReadModelAfterTheReturn.ShouldEqual(Context.ReadModelTheFirstTime);
}
