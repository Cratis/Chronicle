// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_same_observer_is_live_in_another_namespace.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// The unseen observer is one observer in every namespace of its event store. While a client holds it in another namespace it is not
/// removed from the namespace its first client left, and the live client keeps receiving events.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_same_observer_is_live_in_another_namespace(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public int RemovedBySweep;
        public given.Footprint DefaultAfter = given.Footprint.None;
        public given.Footprint OtherAfter = given.Footprint.None;
        public int HandledByTheLiveClient;

        async Task Because()
        {
            var away = new given.UnseenAlphaReactor();
            var live = new given.UnseenAlphaReactor();
            var awayClient = await StartClient(away);
            var liveClient = await StartClient("other", live);
            ObserverId = awayClient.Reactor<given.UnseenAlphaReactor>().Id.Value;

            await GoAway(awayClient, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            RemovedBySweep = await Sweeper.Sweep();
            DefaultAfter = await FootprintOf(ObserverId);
            OtherAfter = await FootprintOf(ObserverId, "other");

            await liveClient.Store.EventLog.Append("source", new given.UnseenEvent("for the live client"));
            await WaitForHandled(live, 1);
            HandledByTheLiveClient = live.Handled.Count;
        }
    }

    [Fact] void should_remove_nothing() => Context.RemovedBySweep.ShouldEqual(0);
    [Fact] void should_keep_the_observer_where_its_client_left() => (Context.DefaultAfter.HasDefinition && Context.DefaultAfter.HasState).ShouldBeTrue();
    [Fact] void should_keep_the_observer_where_its_client_is() => (Context.OtherAfter.HasDefinition && Context.OtherAfter.HasState).ShouldBeTrue();
    [Fact] void should_keep_delivering_to_the_live_client() => Context.HandledByTheLiveClient.ShouldEqual(1);
}
