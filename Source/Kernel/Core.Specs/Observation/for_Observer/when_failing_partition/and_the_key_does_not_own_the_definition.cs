// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_key_does_not_own_the_definition : given.an_observer_under_a_mismatching_key
{
    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Late failure"], "Stack");

    [Fact] void should_not_record_a_failure() => _failedPartitionsState.HasFailedPartitions.ShouldBeFalse();
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_start_recovery_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
