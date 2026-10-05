// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_events_were_not_all_handled : given.a_partition_replay_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = false;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_notify_observer_of_partition_replay_without_proving_any_event_type_replayed() => _observer.Received(1).PartitionReplayed((Key)"some-partition", _lastHandled, Arg.Any<EventSequenceNumber>(), Arg.Is<EventType[]>(_ => _.Length == 0));
    [Fact] void should_not_report_how_far_the_stopped_replay_read() => _observer.Received(1).PartitionReplayed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), EventSequenceNumber.Unavailable, Arg.Any<EventType[]>());
}
