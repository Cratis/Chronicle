// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_the_failed_event_belongs_to_another_source : given.an_observer
{
    static readonly Cratis.Chronicle.Concepts.Keys.Key _partition = "failed-partition";
    static readonly Cratis.Chronicle.Concepts.Events.EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", Cratis.Chronicle.Concepts.Events.EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        GivenFailedEventAt("other-source", 12UL, _eventType);
    }

    async Task Because() => await _observer.ReplayedSuccessfullySince(42UL, new Dictionary<Cratis.Chronicle.Concepts.Keys.Key, Cratis.Chronicle.Concepts.Events.EventSequenceNumber> { [_partition] = 42UL }, [_eventType], DateTimeOffset.UtcNow);

    [Fact] void should_keep_the_failure() => _failedPartitionsStorage.State.Partitions.Single().IsResolved.ShouldBeFalse();
}
