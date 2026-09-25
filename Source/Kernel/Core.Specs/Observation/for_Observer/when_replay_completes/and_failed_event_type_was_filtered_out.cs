// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_failed_event_type_was_filtered_out : given.an_observer
{
    static readonly Key _partition = "failed-partition";
    static readonly EventType _failedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    static readonly EventType _replayedType = new("e9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        GivenFailedEventAt(_partition, 12UL, _failedType);
    }

    async Task Because() => await _observer.ReplayedSuccessfully(42UL, new Dictionary<Key, EventSequenceNumber> { [_partition] = 42UL }, [_replayedType]);

    [Fact] void should_keep_failed_partition() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_keep_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
