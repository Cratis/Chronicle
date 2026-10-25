// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_only_selected_event_types_were_scanned_past_the_held_back_position : given.a_partition_replay_job
{
    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false };
        _stateStorage.State.LastHandledEventSequenceNumber = 40UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 50UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_keep_the_handled_position_and_cap_the_scanned_position() => _observer.Received(1).PartitionReplayed((Key)"some-partition", 40UL, 44UL, Arg.Is<EventType[]>(_ => _.Length == 0));
}
