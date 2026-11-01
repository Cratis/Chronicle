// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_completion_write_fails : given.a_partition_replay_covering_a_failure
{
    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_report_the_failed_write() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retain_a_durable_hold_after_the_fault() => _durablyHeldAfterFault.ShouldBeTrue();
    [Fact] void should_retry_the_failure_store_write() => _failureStorage.PersistedState.Partitions.ShouldBeEmpty();
    [Fact] void should_start_the_successor_from_the_scanned_position() => CheckSuccessor();
    [Fact] void should_decrement_the_count_only_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
