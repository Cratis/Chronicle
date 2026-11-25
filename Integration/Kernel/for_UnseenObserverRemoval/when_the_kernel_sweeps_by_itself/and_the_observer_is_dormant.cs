// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_kernel_sweeps_by_itself.and_the_observer_is_dormant.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_kernel_sweeps_by_itself;

/// <summary>
/// A dormant observer, released from memory while its client stays subscribed, is not removed however many times the sweep runs and however
/// long it is dormant: it is seen through its recorded subscription, and waking it for the question is not needed.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.AutomaticRemovalCollection.Name)]
public class and_the_observer_is_dormant(context context) : Given<context>(context)
{
    public class context(given.AutomaticRemovalFixture fixture) : given.an_unseen_observer_kernel<given.AutomaticRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public bool WasReleased;
        public given.Footprint AfterThePeriods = given.Footprint.None;
        public DateTimeOffset ReleasedAt;
        public DateTimeOffset SeenAfterThePeriods;
        public int ResidentAfterThePeriods;
        public int HandledAfterTheWake;

        async Task Because()
        {
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await WaitForHandledInOrder(alpha, await Append(_store, 1));

            await WaitUntil("the observer to be released from memory", async () => (await ResidentObserverGrains(ObserverId)).Length == 0, 240);
            ReleasedAt = DateTimeOffset.UtcNow;
            WasReleased = true;

            await WaitUntil("three periods to pass while the kernel sweeps every second", () => Task.FromResult(DateTimeOffset.UtcNow > ReleasedAt + (given.UnseenObserverFixture.Period * 3)));
            SeenAfterThePeriods = await LastSeen(ObserverId);
            AfterThePeriods = await FootprintOf(ObserverId);
            ResidentAfterThePeriods = (await ResidentObserverGrains(ObserverId)).Length;

            await Append(_store, 1);
            await WaitForHandled(alpha, 2);
            HandledAfterTheWake = alpha.Handled.Count;
        }

        async Task<string[]> ResidentObserverGrains(string observerId)
        {
            var statistics = await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics();
            var grains = statistics
                .Select(statistic => statistic.GrainId.ToString())
                .Where(grain =>
                    grain.Contains(observerId, StringComparison.OrdinalIgnoreCase) &&
                    grain.Contains(_storeName, StringComparison.Ordinal) &&
                    grain.StartsWith("observer/", StringComparison.OrdinalIgnoreCase));
            return [.. grains];
        }
    }

    [Fact] void should_have_been_released_from_memory() => Context.WasReleased.ShouldBeTrue();
    [Fact] void should_keep_the_definition() => Context.AfterThePeriods.HasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_state() => Context.AfterThePeriods.HasState.ShouldBeTrue();
    [Fact] void should_keep_the_durable_subscription_record() => Context.AfterThePeriods.HasDurableSubscription.ShouldBeTrue();
    [Fact] void should_still_list_the_observer() => Context.AfterThePeriods.IsListed.ShouldBeTrue();
    [Fact] void should_have_recorded_the_client_as_seen_after_it_was_released() => Context.SeenAfterThePeriods.ShouldBeGreaterThan(Context.ReleasedAt);
    [Fact] void should_not_have_woken_the_observer_to_ask() => Context.ResidentAfterThePeriods.ShouldEqual(0);
    [Fact] void should_still_deliver_events_to_its_client() => Context.HandledAfterTheWake.ShouldEqual(2);
}
