// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_a_new_failure_was_recorded_during_replay : given.an_observer
{
    static readonly Key _partition = "failed-partition";
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    DateTimeOffset _replayStartedAt;

    void Establish()
    {
        _replayStartedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        GivenFailedEventAt(_partition, 12UL, _eventType);
    }

    async Task Because() => await _observer.ReplayedSuccessfullySince(42UL, new Dictionary<Key, EventSequenceNumber> { [_partition] = 42UL }, [_eventType], _replayStartedAt);

    [Fact] void should_keep_the_new_failure() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_keep_the_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
