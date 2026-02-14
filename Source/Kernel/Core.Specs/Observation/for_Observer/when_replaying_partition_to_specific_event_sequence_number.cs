// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.Observation.Jobs;
namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_to_specific_event_sequence_number : given.an_observer
{
    static Key _partition;
    static EventSequenceNumber _toSequenceNumber;

    void Establish()
    {
        _partition = "some-partition";
        _toSequenceNumber = 12;
    }

    Task Because() => _observer.ReplayPartitionTo(_partition, _toSequenceNumber);

    [Fact] void should_add_partition_to_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact]
    void should_start_replay_observer_partition_job() => _jobsManager.Received(1).Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
        Arg.Is<ReplayObserverPartitionRequest>(_ => _.FromSequenceNumber == EventSequenceNumber.First && _.ToSequenceNumber == _toSequenceNumber));
}
