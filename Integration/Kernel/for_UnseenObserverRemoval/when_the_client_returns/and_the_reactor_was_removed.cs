// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns.and_the_reactor_was_removed.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns;

/// <summary>
/// After a reactor has been removed the client connects again: the observer is registered as new and replays from the start, so every
/// event appended earlier is handled again, in order.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_reactor_was_removed(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public EventSequenceNumber[] Appended = [];
        public EventSequenceNumber[] HandledTheFirstTime = [];
        public EventSequenceNumber[] HandledAfterTheReturn = [];
        public EventSequenceNumber[] HandledAfterAnotherEvent = [];
        public given.Footprint AfterTheRemoval = given.Footprint.None;
        public given.Footprint AfterTheReturn = given.Footprint.None;
        public int Removed;

        async Task Because()
        {
            var first = new given.UnseenAlphaReactor();
            var client = await StartClient(first);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            Appended = await Append(_store, 3);
            await WaitForHandledInOrder(first, Appended);
            HandledTheFirstTime = [.. first.Handled];

            await GoAway(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);
            Removed = await Sweeper.Sweep();
            AfterTheRemoval = await FootprintOf(ObserverId);

            var again = new given.UnseenAlphaReactor();
            await StartClient(again);
            await WaitForHandledInOrder(again, Appended);
            HandledAfterTheReturn = [.. again.Handled];
            AfterTheReturn = await FootprintOf(ObserverId);

            Appended = [.. Appended, .. await Append(_store, 1)];
            await WaitForHandledInOrder(again, Appended);
            HandledAfterAnotherEvent = [.. again.Handled];
        }
    }

    [Fact] void should_have_removed_the_observer() => Context.Removed.ShouldEqual(1);
    [Fact] void should_have_nothing_left_of_it_after_the_removal() => Context.AfterTheRemoval.ShouldEqual(given.Footprint.None);
    [Fact] void should_handle_every_earlier_event_again_in_order() => Context.HandledAfterTheReturn.ShouldEqual(Context.HandledTheFirstTime);
    [Fact] void should_register_the_observer_as_new() => (Context.AfterTheReturn.HasDefinition && Context.AfterTheReturn.HasState && Context.AfterTheReturn.IsListed).ShouldBeTrue();
    [Fact] void should_handle_a_later_event_after_the_replay() => Context.HandledAfterAnotherEvent.Length.ShouldEqual(4);
    [Fact] void should_handle_a_later_event_in_order() => Context.HandledAfterAnotherEvent.ShouldEqual(Context.Appended);
}
