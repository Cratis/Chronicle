// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States.for_QuarantinedObserver.given;

public class a_quarantined_observer_state : Specification
{
    protected IObserver _observer;
    protected IStateMachine<ObserverState> _stateMachine;
    protected ObserverKey _observerKey;
    protected ObserverState _storedState;
    protected QuarantinedObserver _state;

    void Establish()
    {
        _observer = Substitute.For<IObserver, IStateMachine<ObserverState>>();
        _stateMachine = (IStateMachine<ObserverState>)_observer;
        _observerKey = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        _state = new QuarantinedObserver(_observerKey, Substitute.For<ILogger<QuarantinedObserver>>());
        _state.SetStateMachine(_stateMachine);

        _storedState = new ObserverState
        {
            Identifier = _observerKey.ObserverId,
            RunningState = ObserverRunningState.Quarantined
        };
    }
}
