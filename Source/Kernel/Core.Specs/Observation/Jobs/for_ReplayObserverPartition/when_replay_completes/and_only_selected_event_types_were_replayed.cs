// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_only_selected_event_types_were_replayed : given.a_partition_replay_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;

    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false };
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_not_report_failed_partition_as_recovered() => _observer.DidNotReceive().PartitionReplayed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventType[]>());
    [Fact] void should_report_partition_replay_as_not_proven_complete() => _observer.Received(1).PartitionReplayPartiallyCompleted((Key)"some-partition", _lastHandled);
}
