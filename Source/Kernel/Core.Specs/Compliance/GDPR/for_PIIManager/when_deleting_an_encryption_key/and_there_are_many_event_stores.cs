// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Reaching every event store one at a time made an erasure grow with the number of event stores until it outlasted
/// the grain call timeout. The stores of a phase are reached together; the phases themselves stay strictly ordered.
/// </summary>
public class and_there_are_many_event_stores : given.many_event_stores
{
    readonly Stopwatch _elapsed = new();

    async Task Because()
    {
        _elapsed.Start();
        await _manager.DeleteEncryptionKeyFor(Identifier);
        _elapsed.Stop();
    }

    [Fact] void should_fence_every_event_store() => Reached(Fence).ShouldContainOnly(EventStores);
    [Fact] void should_sweep_every_event_store() => Reached(Sweep).ShouldContainOnly(EventStores);
    [Fact] void should_evict_from_every_event_store() => Reached(Evict).ShouldContainOnly(EventStores);
    [Fact] void should_confirm_every_event_store() => Reached(Confirm).ShouldContainOnly(EventStores);
    [Fact] void should_compact_every_event_store() => Reached(Compact).ShouldContainOnly(EventStores);
    [Fact] void should_fence_every_event_store_before_sweeping_any() => ShouldHaveCompletedBefore(Fence, Sweep);
    [Fact] void should_sweep_every_event_store_before_evicting_from_any() => ShouldHaveCompletedBefore(Sweep, Evict);
    [Fact] void should_evict_from_every_event_store_before_confirming_any() => ShouldHaveCompletedBefore(Evict, Confirm);
    [Fact] void should_confirm_every_event_store_before_compacting_any() => ShouldHaveCompletedBefore(Confirm, Compact);
    [Fact] void should_wait_the_revocation_grace_once() => _delays.ShouldContainOnly(new EncryptionKeyErasureOptions().RevocationGrace);
    [Fact] void should_reach_event_stores_concurrently() => _maximumInFlight.ShouldBeGreaterThan(1);
    [Fact] void should_not_reach_more_event_stores_at_once_than_allowed() => _maximumInFlight.ShouldBeLessThanOrEqual(PIIManager.MaxConcurrentEventStores);

    /// <summary>One store at a time this takes five phases of forty 20 ms steps - four seconds - before the grace.</summary>
    [Fact] void should_not_grow_with_the_number_of_event_stores() => _elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
}
