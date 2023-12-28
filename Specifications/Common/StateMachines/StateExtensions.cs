// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Extension methods for working with states in the context of testing.
/// </summary>
public static class StateExtensions
{
    /// <summary>
    /// Set the state machine for a state.
    /// </summary>
    /// <param name="state">State to set it for.</param>
    /// <param name="stateMachine">The state machine to set.</param>
    /// <typeparam name="TStoredState">Type of stored state.</typeparam>
    public static void SetStateMachine<TStoredState>(this State<TStoredState> state, IStateMachine<TStoredState> stateMachine)
        where TStoredState : class
    {
        state._stateMachine = stateMachine;
    }
}
