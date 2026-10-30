// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_a_stopped_selected_event_type_replay_handled_past_the_held_back_position : given.a_partition_replay_job
{
    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false };
        _stateStorage.State.LastHandledEventSequenceNumber = 50UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 50UL;
        _stateStorage.State.HandledAllEvents = false;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_coverage_ending_before_the_held_back_position() => _observer.Received(1).PartitionReplayed((Key)"some-partition", 44UL, 44UL, Arg.Is<EventType[]>(_ => _.Length == 0));
}
