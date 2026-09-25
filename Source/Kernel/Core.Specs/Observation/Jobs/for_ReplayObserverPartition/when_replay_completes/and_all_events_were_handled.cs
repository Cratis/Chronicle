// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_all_events_were_handled : given.a_partition_replay_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_notify_observer_of_successful_partition_replay() => _observer.Received(1).PartitionReplayed((Key)"some-partition", _lastHandled);
    [Fact] void should_not_notify_observer_of_partial_partition_replay() => _observer.DidNotReceive().PartitionReplayPartiallyCompleted(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}
