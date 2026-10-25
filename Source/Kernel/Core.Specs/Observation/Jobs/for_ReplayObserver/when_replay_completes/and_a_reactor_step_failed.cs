// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reactor_step_failed : given.a_replay_observer_job
{
    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.SuccessfulSteps = 1;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_finalize_the_replay() => _replayServiceClient.Received(1).EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_not_abandon_the_replay() => _replayServiceClient.DidNotReceive().AbandonReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_report_an_incomplete_replay() => _observer.Received(1).Replayed(42UL);
    [Fact] async Task should_not_release_the_observer() => await _observer.DidNotReceive().ReleaseRefusedReplay();
}
