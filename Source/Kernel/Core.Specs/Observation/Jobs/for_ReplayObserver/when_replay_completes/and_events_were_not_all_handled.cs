// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_events_were_not_all_handled : given.a_replay_observer_job
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

    [Fact] void should_notify_observer_of_incomplete_replay() => _observer.Received(1).Replayed(_lastHandled);
    [Fact] void should_not_notify_observer_of_successful_replay() => _observer.DidNotReceive().ReplayedSuccessfully(Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>());
}
