// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_refused_replay_settled_while_generation_initialization_is_quiescing_is_recovered_again : given.a_replaying_projection_recovering_while_generation_initialization_is_quiescing
{
    Exception? _error;

    async Task Establish()
    {
        await Recover();
        await Crash();
        _eventStoreStorage.GenerationInitialization.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => _error = await Catch.Exception(Recover);

    [Fact] void should_not_fail_recovery() => _error.ShouldBeNull();
    [Fact] async Task should_not_enter_replay_again() => await _eventStoreStorage.GenerationInitialization.DidNotReceive().GetReceipts();
    [Fact] async Task should_stay_out_of_the_replay_state() => ((await _observer.GetCurrentState()) is Replay).ShouldBeFalse();
    [Fact] void should_not_be_replaying() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Replaying);
    [Fact] void should_keep_the_replay_flag_cleared() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] async Task should_not_start_a_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
