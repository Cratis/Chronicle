// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_publication_is_resumed_after_reactivation : given.a_successful_reducer_replay
{
    void Establish() => _stateStorage.State.ReducerReplayPhase = ReducerReplayPhase.Publishing;
    async Task Because() => await _job.Resume();
    [Fact] void should_reconcile_the_same_identity_instead_of_rebuilding_or_discarding_it() => _reducerReplay.Received(1).Publish(Arg.Is<ReplayContext>(_ => _.RevertContainerName == _context.RevertContainerName));
    [Fact] void should_not_abandon_the_unknown_swap() => _reducerReplay.DidNotReceive().Abandon(_jobId);
}
