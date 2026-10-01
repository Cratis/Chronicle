// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_reducer_steps_stopped_while_running : given.a_successful_reducer_replay
{
    void Establish()
    {
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.StoppedSteps = 1;
    }
    async Task Because() => await _job.Resume();
    [Fact] void should_not_silently_complete_and_clear_the_interrupted_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_revoke_the_partial_attempt() => _reducerReplay.Received(1).Abandon(_jobId);
    [Fact] void should_not_publish() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
}
