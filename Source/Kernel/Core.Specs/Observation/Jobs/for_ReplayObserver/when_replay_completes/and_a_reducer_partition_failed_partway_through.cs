// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reducer_partition_failed_partway_through : given.a_successful_reducer_replay
{
    void Establish()
    {
        _stateStorage.State.Progress.SuccessfulSteps = 0;
        _stateStorage.State.Progress.FailedSteps = 1;
        _stateStorage.State.HandledAllEvents = false;
    }
    async Task Because() => await _job.Resume();
    [Fact] void should_fail_instead_of_promoting_a_partial_result() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_publish() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
}
