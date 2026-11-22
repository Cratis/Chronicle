// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_a_live_chunk_has_an_unknown_outcome : given.an_observer_with_a_partially_applied_chunk
{
    protected override bool IsLive => true;
    protected override bool HasConfirmedPrefix => false;
    protected override ulong OriginalCount => 1UL;
    protected override ulong FreshCount => 1UL;

    [Fact] void should_surface_the_unknown_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_have_an_unreported_applied_prefix() => _totalAtFailure.ShouldEqual(649UL);
    [Fact] void should_retry_all_unconfirmed_partitions_and_fresh_deliveries() => (_attempts[2].Length + _attempts[3].Length).ShouldEqual(590);
    [Fact] void should_preserve_documented_at_least_once_retries_instead_of_guessing_a_prefix() => _persisted.Values.Aggregate(0UL, (total, count) => total + count).ShouldEqual(1241UL);
    [Fact] void should_not_lose_any_partition() => _persisted.Count.ShouldEqual(1101);
    [Fact] void should_not_increment_running_totals_again() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(1104UL);
}
