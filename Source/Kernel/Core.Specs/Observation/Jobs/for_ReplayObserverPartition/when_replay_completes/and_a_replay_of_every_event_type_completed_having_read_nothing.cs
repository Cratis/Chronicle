// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_a_replay_of_every_event_type_completed_having_read_nothing : given.a_partition_replay_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        _stateStorage.State.LastScannedEventSequenceNumber = EventSequenceNumber.Unavailable;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_no_coverage_and_every_replayed_event_type() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, Arg.Is<EventType[]>(_ => _.SequenceEqual(_request.EventTypes)));
}
