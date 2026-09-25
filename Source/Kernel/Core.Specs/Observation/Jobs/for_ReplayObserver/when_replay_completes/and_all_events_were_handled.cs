// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_all_events_were_handled : given.a_replay_observer_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;
    static readonly Key _failedPartition = "failed-partition";

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = true;
        _observer.GetFailedPartitionKeys().Returns([_failedPartition]);
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_notify_observer_of_successful_replay() => _observer.Received(1).ReplayedSuccessfully(
        _lastHandled, Arg.Is<IReadOnlyDictionary<Key, EventSequenceNumber>>(_ => _[_failedPartition] == _lastHandled), Arg.Any<EventType[]>());
    [Fact] void should_not_notify_observer_of_partial_replay() => _observer.DidNotReceive().Replayed(Arg.Any<EventSequenceNumber>());
}
