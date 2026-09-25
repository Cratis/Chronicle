// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_all_events_were_handled : given.an_observer
{
    static readonly Key _partition = "some-partition";
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        _failedPartitionsStorage.State.Quarantine(_partition);
        GivenFailedEventAt(_partition, 12UL, _eventType);
    }

    async Task Because() => await _observer.ReplayedSuccessfully(42UL, new Dictionary<Key, EventSequenceNumber> { [_partition] = 42UL }, [_eventType]);

    [Fact] void should_resolve_failed_partition() => _failedPartitionsStorage.State.ResolvedPartitions.Single().IsResolved.ShouldBeTrue();
    [Fact] void should_remove_failed_partition() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_decrement_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_persist_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
}
