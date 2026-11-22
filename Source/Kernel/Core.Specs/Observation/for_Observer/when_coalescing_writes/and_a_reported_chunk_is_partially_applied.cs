// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_coalescing_writes;

public class and_a_reported_chunk_is_partially_applied : given.an_observer_with_a_partially_applied_chunk
{
    async Task Because()
    {
        var flush = Catch.Exception(_observer.RunWatchdogAsync);
        try
        {
            await _writeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
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
    [Fact] void should_keep_the_confirmed_prefix_total() => _totalAtFailure.ShouldEqual(1298UL);
    [Fact] void should_advance_running_totals_only_for_confirmed_reports() => _runningTotalAtFailure.ShouldEqual(1298UL);
    [Fact] void should_retry_only_the_suffix_and_fresh_reports() => _attempts[2].Length.ShouldEqual(454);
    [Fact] void should_merge_fresh_reports_in_already_applied_and_pending_partitions() => EveryPartitionHasItsExactCount().ShouldBeTrue();
    [Fact] void should_persist_the_fresh_partition() => _persisted[(Key)"fresh"].ShouldEqual(5UL);
    [Fact] void should_finish_with_exact_running_totals() => _stateStorage.State.HandledEventCount.Value.ShouldEqual(2220UL);
    [Fact] void should_keep_only_confirmed_reports_in_the_per_type_total() => _perTypeAtFailure.ShouldEqual(1298UL);
    [Fact] void should_finish_with_exact_per_type_totals() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].Value.ShouldEqual(2220UL);
    [Fact] void should_finish_in_three_attempts() => _attempts.Count.ShouldEqual(3);
}
