// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_projection_step_failed : given.a_replay_observer_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.SuccessfulSteps = 0;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_still_finalize_the_replay() => _replayServiceClient.Received(1).EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_not_abandon_the_replay() => _replayServiceClient.DidNotReceive().AbandonReplayFor(Arg.Any<ObserverDetails>());
}
