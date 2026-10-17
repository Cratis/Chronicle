// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition : given.an_observer
{
    static Key _partition;

    void Establish() => _partition = "some-partition";

    Task Because() => _observer.ReplayPartition(_partition);

    [Fact] void should_add_partition_to_replaying_partitions() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_start_the_job_with_a_wildcard_scope_for_an_observer_with_no_event_types() => _jobsManager.Received(1).Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
        Arg.Is<ReplayObserverPartitionRequest>(_ => _.ReaderScope!.IsWildcard));
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact]
    void should_start_replay_observer_partition_job() => _jobsManager.Received(1).Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
        Arg.Is<ReplayObserverPartitionRequest>(_ => _.FromSequenceNumber == EventSequenceNumber.First && _.ToSequenceNumber == EventSequenceNumber.Max));
}
