// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.when_entering;

public class and_subscribers_event_types_are_different_and_replay_evaluator_returns_false : given.a_routing_state_and_event_types_that_are_different_from_subscription
{
    void Establish()
    {
        _replayEvaluator.Evaluate(Arg.Any<ReplayEvaluationContext>()).Returns(false);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_only_perform_one_transition() => _observer.Received(1).TransitionTo<IState<ObserverState>>();
    [Fact] void should_transition_to_replay() => _observer.Received(1).TransitionTo<Observing>();
}
