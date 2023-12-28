// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Exception that gets thrown when a type is not a valid type for a state machine.
/// </summary>
public class UnknownStateTypeInStateMachine : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownStateTypeInStateMachine"/> class.
    /// </summary>
    /// <param name="type">State type that is unknown.</param>
    /// <param name="stateMachineType">The state machine that the type is unknown for.</param>
    public UnknownStateTypeInStateMachine(Type type, Type stateMachineType)
        : base($"Type '{type.FullName}' is unknown in the state machine '{stateMachineType.FullName}'")
    {
    }
}
