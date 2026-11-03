// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation.States.for_Replay.given;

public class a_replay_state_with_ownership_claims : a_replay_state
{
    protected IObserverOwnershipClaims _claims;

    void Establish()
    {
        _observer = Substitute.For<IObserver, IStateMachine<ObserverState>, IObserverOwnershipClaims>();
        _stateMachine = (IStateMachine<ObserverState>)_observer;
        _claims = (IObserverOwnershipClaims)_observer;
        _observer.GetSubscription().Returns(_ => Task.FromResult(_subscription));
        _state.SetStateMachine(_stateMachine);
    }
}
