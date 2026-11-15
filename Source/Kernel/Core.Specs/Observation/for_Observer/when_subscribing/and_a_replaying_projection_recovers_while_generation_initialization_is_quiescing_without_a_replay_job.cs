// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_replaying_projection_recovers_while_generation_initialization_is_quiescing_without_a_replay_job : given.a_replaying_projection_recovering_while_generation_initialization_is_quiescing
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(Recover);

    [Fact] void should_not_fail_recovery() => _error.ShouldBeNull();
    [Fact] async Task should_leave_the_replay_state() => ((await _observer.GetCurrentState()) is Replay).ShouldBeFalse();
    [Fact] void should_not_be_replaying() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Replaying);
    [Fact] void should_clear_the_replay_flag() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] void should_release_the_refused_replay() => ReleasedTheRefusedReplay.ShouldBeTrue();
    [Fact] void should_record_the_refusal() => RecordedTheRefusal.ShouldBeTrue();
    [Fact] void should_preserve_the_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] async Task should_not_start_a_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_reset_the_handled_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
}
