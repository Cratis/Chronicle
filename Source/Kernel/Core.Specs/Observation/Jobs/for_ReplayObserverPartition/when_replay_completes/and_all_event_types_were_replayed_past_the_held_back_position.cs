// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_all_event_types_were_replayed_past_the_held_back_position : given.a_partition_replay_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 50UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 52UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_the_full_coverage() => _observer.Received(1).PartitionReplayed((Key)"some-partition", 50UL, 52UL, Arg.Is<EventType[]>(_ => _.SequenceEqual(_request.EventTypes)));
}
