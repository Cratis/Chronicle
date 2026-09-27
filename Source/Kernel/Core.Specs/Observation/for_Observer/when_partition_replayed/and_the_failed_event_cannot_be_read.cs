// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_the_failed_event_cannot_be_read : given.an_observer_with_replaying_partition
{
    static readonly EventType _failedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        _eventSequenceStorage.GetEventAt(12UL).Returns<Task<AppendedEvent>>(_ => throw new InvalidOperationException("Event is unavailable"));
        EventSequenceDoesNotHaveNextEvent(_lastHandledEventSequenceNumber);
    }

    async Task Because() => await _observer.PartitionReplayed(_partition, _lastHandledEventSequenceNumber, [_failedType]);

    [Fact] void should_keep_the_failure() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_clear_the_replaying_partition() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
}
