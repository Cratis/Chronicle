// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_reading_state_with_partitions_of_another_sequence : given.the_provider
{
    static readonly ObserverKey _observerKey = new("an-observer", "an-event-store", "a-namespace", "inbox-another-store");
    FailedPartition _untagged;
    FailedPartition _onThisSequence;
    FailedPartition _onAnotherSequence;
    GrainState<FailedPartitions> _state;

    void Establish()
    {
        _untagged = new() { ObserverId = _observerKey.ObserverId };
        _onThisSequence = new() { ObserverId = _observerKey.ObserverId, EventSequenceId = _observerKey.EventSequenceId };
        _onAnotherSequence = new() { ObserverId = _observerKey.ObserverId, EventSequenceId = EventSequenceId.Log };
        _failedPartitionsStorage.GetFor(_observerKey.ObserverId).Returns(new FailedPartitions { Partitions = [_untagged, _onThisSequence, _onAnotherSequence] });
        _state = new();
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _observerKey.ToString()), _state);

    [Fact] void should_load_the_untagged_partition() => _state.State!.Partitions.ShouldContain(_untagged);
    [Fact] void should_load_the_partition_of_this_sequence() => _state.State!.Partitions.ShouldContain(_onThisSequence);
    [Fact] void should_not_load_the_partition_of_another_sequence() => _state.State!.Partitions.ShouldNotContain(_onAnotherSequence);
}
