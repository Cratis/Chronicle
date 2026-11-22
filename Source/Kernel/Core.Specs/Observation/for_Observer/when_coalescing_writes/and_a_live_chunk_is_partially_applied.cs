// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_a_live_chunk_is_partially_applied : given.an_observer_with_a_partially_applied_chunk
{
    protected override bool IsLive => true;
    protected override ulong OriginalCount => 1UL;
    protected override ulong FreshCount => 1UL;

    async Task Because()
    {
        var flush = Catch.Exception(() => _observer.RunWatchdogAsync());
        try
        {
            await _writeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            for (var index = 0; index < _freshPartitions.Count; index++)
            {
                await Deliver(_freshPartitions[index], (ulong)(1100 + index), FreshCount);
            }
        }
        finally
        {
            _releaseWrite.TrySetResult();
        }

        _error = await flush;
        _totalAtFailure = _persisted.Values.Aggregate(0UL, (total, count) => total + count);
        _runningTotalAtFailure = _stateStorage.State.HandledEventCount.Value;
        _perTypeAtFailure = _stateStorage.State.HandledEventCountPerEventType[event_type.Id].Value;
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_surface_the_partial_failure() => _error.ShouldBeOfExactType<ObserverHandledCountsPartiallyIncremented>();
    [Fact] void should_keep_the_confirmed_prefix_total() => _totalAtFailure.ShouldEqual(649UL);
    [Fact] void should_not_advance_live_running_totals_at_flush_time() => _runningTotalAtFailure.ShouldEqual(1104UL);
    [Fact] void should_retry_only_the_suffix_and_fresh_deliveries() => _attempts[2].Length.ShouldEqual(454);
    [Fact] void should_merge_fresh_deliveries_without_replaying_the_original_prefix() => EveryPartitionHasItsExactCount().ShouldBeTrue();
    [Fact] void should_persist_the_fresh_partition() => _persisted[(Key)"fresh"].ShouldEqual(1UL);
    [Fact] void should_not_double_count_live_running_totals() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(1104UL);
    [Fact] void should_leave_successful_deliveries_out_of_in_flight_recovery() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_keep_live_per_type_totals_unchanged_at_flush_time() => _perTypeAtFailure.ShouldEqual(1104UL);
    [Fact] void should_finish_with_exact_per_type_totals() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].Value.ShouldEqual(1104UL);
    [Fact] void should_finish_in_three_attempts() => _attempts.Count.ShouldEqual(3);
}
