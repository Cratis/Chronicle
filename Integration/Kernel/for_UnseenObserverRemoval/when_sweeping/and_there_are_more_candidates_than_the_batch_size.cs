// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_there_are_more_candidates_than_the_batch_size.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// The sweep reads observers in batches of the configured size, two here, and three are unseen: all of them are removed, and
/// sweeping again removes nothing more.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_there_are_more_candidates_than_the_batch_size(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string[] ObserverIds = [];
        public int RemovedByTheFirstSweep;
        public int RemovedByTheSecondSweep;
        public given.Footprint[] After = [];

        async Task Because()
        {
            var reactors = new object[] { new given.UnseenAlphaReactor(), new given.UnseenBetaReactor(), new given.UnseenGammaReactor() };
            var client = await StartClient(reactors);
            ObserverIds =
            [
                client.Reactor<given.UnseenAlphaReactor>().Id.Value,
                client.Reactor<given.UnseenBetaReactor>().Id.Value,
                client.Reactor<given.UnseenGammaReactor>().Id.Value
            ];
            await GoAway(client, ObserverIds[0]);
            foreach (var id in ObserverIds)
            {
                await WaitUntil($"{id} to be unsubscribed", () => IsUnsubscribed(id));
                await WaitUntilUnseenForThePeriod(id);
            }

            RemovedByTheFirstSweep = await Sweeper.Sweep();
            After = await Task.WhenAll(ObserverIds.Select(id => FootprintOf(id)));
            RemovedByTheSecondSweep = await Sweeper.Sweep();
        }
    }

    [Fact] void should_remove_all_three() => Context.RemovedByTheFirstSweep.ShouldEqual(3);
    [Fact] void should_leave_nothing_of_any_of_them() => Context.After.ShouldEqual([given.Footprint.None, given.Footprint.None, given.Footprint.None]);
    [Fact] void should_remove_nothing_more_the_second_time() => Context.RemovedByTheSecondSweep.ShouldEqual(0);
}
