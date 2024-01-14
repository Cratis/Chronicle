// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Kernel.Storage.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.when_entering;

public class and_subscribers_event_types_are_different_and_replay_evaluator_returns_false : given.a_routing_state_and_event_types_that_are_different_from_subscription
{
    void Establish()
    {
        replay_evaluator.Setup(_ => _.Evaluate(IsAny<ReplayEvaluationContext>())).Returns(() => Task.FromResult(false));
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => observer.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_replay() => observer.Verify(_ => _.TransitionTo<Observing>(), Once());
}
