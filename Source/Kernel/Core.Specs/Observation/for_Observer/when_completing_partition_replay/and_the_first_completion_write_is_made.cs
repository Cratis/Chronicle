// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_first_completion_write_is_made : given.a_partition_replay_covering_a_failure
{
    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because() => await CompleteWithFault();

    [Fact] void should_retain_the_replay_marker() => _completionStorage.PersistedState.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_persist_the_partition() => _completionStorage.PersistedState.PartitionReplayCompletions!.Single().Partition.ShouldEqual(_partition);
    [Fact] void should_persist_the_handled_position() => _completionStorage.PersistedState.PartitionReplayCompletions!.Single().LastHandled.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_persist_the_scanned_boundary() => _completionStorage.PersistedState.PartitionReplayCompletions!.Single().Boundary.ShouldEqual((EventSequenceNumber)50UL);
    [Fact] void should_persist_the_covered_episode() => _completionStorage.PersistedState.PartitionReplayCompletions!.Single().CoveredEpisode.ShouldEqual(_failureStorage.PersistedState.Partitions.Single().Id);
    [Fact] void should_record_when_completion_was_received() => (_completionStorage.PersistedState.PartitionReplayCompletions!.Single().Recorded > DateTimeOffset.UnixEpoch).ShouldBeTrue();
}
