// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reducer_step_failed_to_start : given.a_successful_reducer_replay
{
    void Establish()
    {
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.FailedSteps = 1;
    }
    async Task Because() => await _job.Resume();
    [Fact] void should_retain_a_failed_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_revoke_publication_despite_the_other_successful_result() => _reducerReplay.Received(1).Abandon(_jobId);
    [Fact] void should_not_publish_the_partial_model() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
}
