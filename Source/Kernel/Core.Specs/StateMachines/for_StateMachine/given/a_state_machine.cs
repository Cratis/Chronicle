// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.StateMachines.given;

public abstract class a_state_machine : Specification
{
#if NET8_0
    static readonly object _lockObject = new();
#else
    static readonly Lock _lockObject = new();
#endif
    StateMachineForTesting? _stateMachinePrivate;

    protected StateMachineForTesting StateMachine
    {
        get
        {
            lock (_lockObject)
            {
                _stateMachinePrivate ??= _silo.CreateGrainAsync<StateMachineForTesting>(IdSpan.Create(string.Empty)).GetAwaiter().GetResult();
                return _stateMachinePrivate;
            }
        }
    }
    protected virtual Type? InitialState => null;
    protected IStorage<StateMachineStateForTesting> _stateStorage;
    protected TestKitSilo _silo = new();

    void Establish()
    {
        var states = CreateStates();
        _silo.AddService(states);
        _silo.AddService(InitialState ?? typeof(NoOpState<StateMachineStateForTesting>));

        _stateStorage = _silo.StorageManager.GetStorage<StateMachineStateForTesting>(typeof(StateMachineForTesting).FullName);
    }

    protected abstract IEnumerable<IState<StateMachineStateForTesting>> CreateStates();
}
