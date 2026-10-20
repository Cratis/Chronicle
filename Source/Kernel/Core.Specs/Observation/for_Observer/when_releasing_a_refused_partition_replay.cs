// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_partition_replay : when_partition_replayed.given.an_observer_with_replaying_partition
{
    void Establish() => EventSequenceHasNextEvent(_nextEventSequenceNumber - 1);

    async Task Because()
    {
        await _observer.ReleaseRefusedPartitionReplay(_partition);
        await _observer.ReleaseRefusedPartitionReplay(_partition);
    }

    [Fact] void should_remove_the_partition_replay_marker() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_handover_to_catchup() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextEventSequenceNumber);
    [Fact] void should_start_catchup_at_the_retained_next_position() => CheckStartedCatchupJob(_nextEventSequenceNumber - 1);
    [Fact] void should_not_resolve_failures() => _failedPartitionsStorage.State.ResolvedPartitions.ShouldBeEmpty();
    [Fact] void should_persist_release_and_catchup() => _storageStats.Writes.ShouldEqual(2);
}
