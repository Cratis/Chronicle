// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_a_selected_event_type_replay_read_nothing_while_events_were_held_back : given.a_partition_replay_job
{
    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false };
        _stateStorage.State.LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        _stateStorage.State.LastScannedEventSequenceNumber = EventSequenceNumber.Unavailable;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_coverage_ending_before_the_held_back_position() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, 44UL, Arg.Is<EventType[]>(_ => _.Length == 0));
}
