// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_finalization_fails : given.a_replay_observer_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _replayServiceClient.EndReplayFor(Arg.Any<ObserverDetails>()).Returns(Task.FromException(new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown)));
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_not_report_a_successful_replay() => _observer.DidNotReceive().ReplayedSuccessfullySince(
        Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
    [Fact] void should_report_an_incomplete_replay() => _observer.Received(1).Replayed(42UL);
}
