// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

/// <summary>
/// Regression for #4592: one failed partition with an unhandled event behind the observer, plus a live event.
/// Catch-up has nothing to do for the failed partition, so routing must settle into observing once and start no catch-up job.
/// </summary>
public class and_only_a_failed_partition_has_unhandled_events : given.a_routing_state_with_a_failed_partition
{
    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_catch_up() => _observer.DidNotReceive().CatchUp();
    [Fact] void should_transition_once() => _stateMachine.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_transition_to_observing() => _stateMachine.Received(1).TransitionTo<Observing>();
    [Fact] void should_move_the_position_past_the_tail() => _resultingStoredState.NextEventSequenceNumber.ShouldEqual(_tailEventSequenceNumbers.Tail.Next());
}
