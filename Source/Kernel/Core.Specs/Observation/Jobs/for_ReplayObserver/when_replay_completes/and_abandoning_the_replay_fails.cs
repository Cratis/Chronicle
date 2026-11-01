// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_abandoning_the_replay_fails : given.a_replay_observer_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.SuccessfulSteps = 0;
        _replayServiceClient.AbandonReplayFor(Arg.Any<ObserverDetails>()).Returns(Task.FromException(new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown)));
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_not_promote_the_rebuilt_read_model() => _replayServiceClient.DidNotReceive().EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
    [Fact] void should_not_report_an_incomplete_replay() => _observer.DidNotReceive().Replayed(Arg.Any<EventSequenceNumber>());
}
