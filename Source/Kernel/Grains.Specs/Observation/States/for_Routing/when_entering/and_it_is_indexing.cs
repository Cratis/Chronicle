// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States.for_Routing.when_entering;

public class and_it_is_indexing : given.a_routing_state
{
    void Establish() => stored_state = stored_state with { RunningState = ObserverRunningState.Indexing };

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => observer.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_indexing() => observer.Verify(_ => _.TransitionTo<Indexing>(), Once());
}
