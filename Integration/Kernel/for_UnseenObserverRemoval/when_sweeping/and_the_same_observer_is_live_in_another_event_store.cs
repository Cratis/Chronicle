// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_same_observer_is_live_in_another_event_store.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// Another event store has an observer with the same id and a client that is still there: only the event store of the unseen observer
/// is affected.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_same_observer_is_live_in_another_event_store(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public string OtherStore = string.Empty;
        public int RemovedBySweep;
        public given.Footprint UnseenAfter = given.Footprint.None;
        public bool OtherHasDefinition;
        public bool OtherHasState;
        public int HandledByTheLiveClient;

        async Task Because()
        {
            OtherStore = $"{StoreName}-other";
            var away = new given.UnseenAlphaReactor();
            var live = new given.UnseenAlphaReactor();
            var awayClient = await StartClient(away);
            var liveClient = await StartClient(OtherStore, "Default", live);
            ObserverId = awayClient.Reactor<given.UnseenAlphaReactor>().Id.Value;
            liveClient.Reactor<given.UnseenAlphaReactor>().Id.Value.ShouldEqual(ObserverId);

            await GoAway(awayClient, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            RemovedBySweep = await Sweeper.Sweep();
            UnseenAfter = await FootprintOf(ObserverId);
            OtherHasDefinition = await KernelStorage.GetEventStore(OtherStore).Observers.Has(ObserverId);
            OtherHasState = (await KernelStorage.GetEventStore(OtherStore).GetNamespace("Default").Observers.Get(ObserverId)).Identifier == ObserverId;

            await liveClient.Store.EventLog.Append("source", new given.UnseenEvent("for the live client"));
            await WaitForHandled(live, 1);
            HandledByTheLiveClient = live.Handled.Count;
        }
    }

    [Fact] void should_remove_exactly_one_observer() => Context.RemovedBySweep.ShouldEqual(1);
    [Fact] void should_leave_nothing_of_the_unseen_observer() => Context.UnseenAfter.ShouldEqual(given.Footprint.None);
    [Fact] void should_keep_the_definition_in_the_other_event_store() => Context.OtherHasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_state_in_the_other_event_store() => Context.OtherHasState.ShouldBeTrue();
    [Fact] void should_keep_delivering_to_the_live_client() => Context.HandledByTheLiveClient.ShouldEqual(1);
}
