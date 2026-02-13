// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines;

/// <summary>
/// Exception that gets thrown when the current state is unknown.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownCurrentState"/> class.
/// </remarks>
/// <param name="currentState">Current state that is unknown.</param>
/// <param name="stateMachineType">The state machine that the current state is unknown for.</param>
public class UnknownCurrentState(string currentState, Type stateMachineType) : Exception($"Current state '{currentState}' is unknown for the state machine '{stateMachineType.FullName}'")
{
}
