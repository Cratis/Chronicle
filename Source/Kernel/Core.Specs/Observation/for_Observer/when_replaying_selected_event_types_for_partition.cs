// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_selected_event_types_for_partition : given.an_observer
{
    static readonly Key _partition = "some-partition";
    static readonly EventType _affectedEventType = new("affected", 1);
    static readonly EventTypeId _unaffectedEventTypeId = "unaffected";

    void Establish() => _observerHandledCountsStorage
        .GetFor(_observerId, _partition)
        .Returns(new Dictionary<EventTypeId, EventCount>
        {
            [_affectedEventType.Id] = 2,
            [_unaffectedEventTypeId] = 3
        });

    Task Because() => _observer.ReplayPartition(_partition, [_affectedEventType]);

    [Fact]
    void should_only_replay_the_selected_event_type() => _jobsManager.Received(1).Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
        Arg.Is<ReplayObserverPartitionRequest>(_ => _.EventTypes.SequenceEqual(new[] { _affectedEventType })));

    [Fact]
    void should_retain_counts_for_unaffected_event_types() => _observerHandledCountsStorage.Received(1).Increment(
        _observerId,
        _partition,
        Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(_ => _.Count == 1 && _[_unaffectedEventTypeId] == (EventCount)3UL));
}
