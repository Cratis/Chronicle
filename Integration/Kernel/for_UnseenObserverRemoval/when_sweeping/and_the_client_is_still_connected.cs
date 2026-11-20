// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_client_is_still_connected.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// An observer whose client still holds the subscription is not removed however old its recorded time is, and it keeps handling events.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_client_is_still_connected(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public int RemovedBySweep;
        public given.Footprint After = given.Footprint.None;
        public DateTimeOffset BackdatedTo;
        public DateTimeOffset SeenAfterTheSweep;
        public int HandledAfterTheSweep;

        async Task Because()
        {
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            var appended = await Append(_store, 2);
            await WaitForHandledInOrder(alpha, appended);

            await Backdate(ObserverId, TimeSpan.FromDays(365));
            BackdatedTo = await LastSeen(ObserverId);
            RemovedBySweep = await Sweeper.Sweep();
            After = await FootprintOf(ObserverId);
            SeenAfterTheSweep = await LastSeen(ObserverId);

            appended = [.. appended, .. await Append(_store, 1)];
            await WaitForHandledInOrder(alpha, appended);
            HandledAfterTheSweep = alpha.Handled.Count;
        }
    }

    [Fact] void should_have_looked_a_year_unseen() => (Context.SeenAfterTheSweep - Context.BackdatedTo).ShouldBeGreaterThan(TimeSpan.FromDays(364));
    [Fact] void should_remove_nothing() => Context.RemovedBySweep.ShouldEqual(0);
    [Fact] void should_keep_the_definition() => Context.After.HasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_state() => Context.After.HasState.ShouldBeTrue();
    [Fact] void should_still_list_the_observer() => Context.After.IsListed.ShouldBeTrue();
    [Fact] void should_see_the_client_again() => (DateTimeOffset.UtcNow - Context.SeenAfterTheSweep).ShouldBeLessThan(given.UnseenObserverFixture.Period);
    [Fact] void should_keep_handling_events() => Context.HandledAfterTheSweep.ShouldEqual(3);
}
