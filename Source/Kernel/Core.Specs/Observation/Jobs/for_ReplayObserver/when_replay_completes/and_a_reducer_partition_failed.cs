// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reducer_partition_failed : given.a_replay_observer_job
{
    static readonly EventSequenceNumber _lastHandled = 42UL;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.LastHandledEventSequenceNumber = _lastHandled;
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.SuccessfulSteps = 1;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_abandon_the_replay() => _replayServiceClient.Received(1).AbandonReplayFor(Arg.Is<ObserverDetails>(_ => _.Type == ObserverType.Reducer));
    [Fact] void should_not_promote_the_rebuilt_read_model() => _replayServiceClient.DidNotReceive().EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_notify_observer_of_incomplete_replay() => _observer.Received(1).Replayed(_lastHandled);
    [Fact] void should_not_notify_observer_of_successful_replay() => _observer.DidNotReceive().ReplayedSuccessfullySince(Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
}
