// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

public class and_a_partition_fails : given.an_observer_under_a_mismatching_key
{
    static readonly Key _partition = "a-partition";

    async Task Because() => await _observer.PartitionFailed(_partition, 42UL, ["error"], "stack trace");

    [Fact] void should_not_write_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_save_the_failed_partition_to_storage() => await _eventStoreNamespaceStorage.FailedPartitions.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
    [Fact] void should_not_write_state() => _persistedStateStorage!.Stats.Writes.ShouldEqual(0);
    [Fact] void should_not_register_a_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}
