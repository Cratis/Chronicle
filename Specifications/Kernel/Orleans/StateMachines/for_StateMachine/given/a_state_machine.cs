// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.Runtime;
using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.given;

public abstract class a_state_machine : Specification
{
    object lock_object = new();
    StateMachineForTesting? state_machine_private;

    protected StateMachineForTesting state_machine
    {
        get
        {
            lock (lock_object)
            {
                state_machine_private ??= silo.CreateGrainAsync<StateMachineForTesting>(IdSpan.Create(string.Empty)).GetAwaiter().GetResult();
                return state_machine_private;
            }
        }
    }
    protected virtual Type? initial_state => default;
    protected IStorage<StateMachineState> state_storage;

    protected TestKitSilo silo = new();

    void Establish()
    {
        var states = CreateStates();
        silo.AddService(states);
        silo.AddService(initial_state);

        state_storage = silo.StorageManager.GetStorage<StateMachineState>();
    }

    protected abstract IEnumerable<IState<StateMachineState>> CreateStates();
}
