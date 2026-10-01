// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_the_successful_reducer_result_was_not_persisted : given.a_successful_reducer_replay
{
    void Establish() => _stateStorage.State.ReducerReplayContext = null;
    async Task Because() => await _job.Resume();
    [Fact] void should_not_treat_a_step_count_as_proof_of_publication() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_publish_a_target_without_its_successful_result() => _reducerReplay.DidNotReceive().Publish(Arg.Any<ReplayContext>());
}
