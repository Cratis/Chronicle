// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_a_legacy_selected_event_type_replay_cannot_read_the_observed_event_types : given.a_partition_replay_job
{
    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false, HeldBackFrom = EventSequenceNumber.Unavailable };
        _observer.GetEventTypes().Returns(Task.FromException<IEnumerable<EventType>>(new InvalidOperationException("Observer unavailable")));
        _stateStorage.State.LastHandledEventSequenceNumber = 50UL;
        _stateStorage.State.LastScannedEventSequenceNumber = 50UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_no_coverage_so_catch_up_starts_from_the_first_position() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, Arg.Is<EventType[]>(_ => _.Length == 0));
    [Fact] void should_not_report_the_handled_position_as_covered() => _observer.DidNotReceive().PartitionReplayed(Arg.Any<Key>(), 50UL, Arg.Any<EventSequenceNumber>(), Arg.Any<EventType[]>());
}
