// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_kernel_sweeps_by_itself.and_nobody_calls_the_sweep.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_kernel_sweeps_by_itself;

/// <summary>
/// With the removal turned on, the sweep every silo starts with itself finds an observer whose client has been away for longer than the
/// period and removes it: nothing in the specification asks for a sweep.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.AutomaticRemovalCollection.Name)]
public class and_nobody_calls_the_sweep(context context) : Given<context>(context)
{
    public class context(given.AutomaticRemovalFixture fixture) : given.an_unseen_observer_kernel<given.AutomaticRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public given.Footprint Before = given.Footprint.None;
        public given.Footprint After = given.Footprint.None;
        public long CountedBefore;
        public long CountedAfter;

        async Task Because()
        {
            CountedBefore = CountedRemovals;
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await WaitForHandledInOrder(alpha, await Append(_store, 2));
            Before = await FootprintOf(ObserverId);

            await GoAway(client, ObserverId);
            await WaitUntil("the kernel to remove the observer by itself", async () => !(await FootprintOf(ObserverId)).HasDefinition);
            After = await FootprintOf(ObserverId);
            CountedAfter = CountedRemovals;
        }
    }

    [Fact] void should_have_had_the_observer_before() => (Context.Before.HasDefinition && Context.Before.HasState && Context.Before.IsListed).ShouldBeTrue();
    [Fact] void should_leave_nothing_of_it() => Context.After.ShouldEqual(given.Footprint.None);
    [Fact] void should_count_the_removal() => (Context.CountedAfter - Context.CountedBefore).ShouldEqual(1);
}
