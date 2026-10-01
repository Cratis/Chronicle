// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_stopping;

public class and_a_reducer_step_finishes_cancellation : given.a_successful_reducer_replay
{
    async Task Establish()
    {
        _stateStorage.State.Progress.SuccessfulSteps = 0;
        await _job.Stop();
    }
    async Task Because() => await _job.OnStepStopped(JobStepId.New(), JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
    [Fact] void should_remain_stopped_for_resume() => _stateStorage.State.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_revoke_the_interrupted_attempt() => _reducerReplay.Received(1).Abandon(_jobId);
    [Fact] void should_not_publish_on_stop() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
}
