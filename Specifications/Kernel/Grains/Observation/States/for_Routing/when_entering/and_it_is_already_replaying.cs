// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.when_entering;

public class and_it_is_already_replaying : given.a_routing_state
{
    void Establish() => stored_state.RunningState = ObserverRunningState.Replaying;

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => state_machine.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_replay() => state_machine.Verify(_ => _.TransitionTo<Replay>(), Once());
}
