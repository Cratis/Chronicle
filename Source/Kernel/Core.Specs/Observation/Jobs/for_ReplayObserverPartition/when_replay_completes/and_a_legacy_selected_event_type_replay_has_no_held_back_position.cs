// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_a_legacy_selected_event_type_replay_has_no_held_back_position : given.a_partition_replay_job
{
    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false, HeldBackFrom = EventSequenceNumber.Unavailable };
        _observer.GetEventTypes().Returns(Task.FromResult<IEnumerable<EventType>>([.. _request.EventTypes, new EventType("other-event", EventTypeGeneration.First)]));
        _stateStorage.State.LastHandledEventSequenceNumber = 50UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 50UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_no_coverage() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, Arg.Is<EventType[]>(_ => _.Length == 0));
}
