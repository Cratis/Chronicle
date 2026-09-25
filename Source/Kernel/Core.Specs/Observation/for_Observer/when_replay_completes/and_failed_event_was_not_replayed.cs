// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_failed_event_was_not_replayed : given.an_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition((Key)"some-partition", 50UL);
    }

    async Task Because() => await _observer.ReplayedSuccessfully(42UL);

    [Fact] void should_keep_failed_partition() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_keep_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_not_persist_failures() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
}
