// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_two_silos_sweep_at_once.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// Two sweepers, as on two silos, run at the same moment over the same storage: every unseen observer is removed, nothing is left of
/// any of them, what the sweepers report is what is counted, and a sweep after that finds nothing. Silos do not coordinate, so an
/// observer both reach in the same instant can be reported by both: at most twice, never more, and never left half removed.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_two_silos_sweep_at_once(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string[] ObserverIds = [];
        public int[] Removed = [];
        public int RemovedByALaterSweep;
        public long CountedBefore;
        public long CountedAfter;
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

            var secondSilo = ActivatorUtilities.CreateInstance<UnseenObserverRemoval>(Services);
            CountedBefore = CountedRemovals;
            Removed = await Task.WhenAll(Task.Run(() => Sweeper.Sweep()), Task.Run(() => secondSilo.Sweep()));
            CountedAfter = CountedRemovals;
            After = await Task.WhenAll(ObserverIds.Select(id => FootprintOf(id)));
            RemovedByALaterSweep = await Sweeper.Sweep();
        }
    }

    [Fact] void should_remove_every_observer_between_them() => Context.Removed.Sum().ShouldBeGreaterThanOrEqual(3);
    [Fact] void should_report_no_observer_more_than_once_by_each_silo() => Context.Removed.Sum().ShouldBeLessThanOrEqual(6);
    [Fact] void should_count_what_the_sweepers_reported() => (Context.CountedAfter - Context.CountedBefore).ShouldEqual(Context.Removed.Sum());
    [Fact] void should_leave_nothing_of_any_of_them() => Context.After.ShouldEqual([given.Footprint.None, given.Footprint.None, given.Footprint.None]);
    [Fact] void should_find_nothing_to_remove_afterwards() => Context.RemovedByALaterSweep.ShouldEqual(0);
}
