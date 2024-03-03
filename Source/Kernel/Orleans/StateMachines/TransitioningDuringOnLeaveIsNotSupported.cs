// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Exception that gets thrown when a state tries to transition to another state during OnLeave.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransitioningDuringOnLeaveIsNotSupported"/> class.
/// </remarks>
/// <param name="stateType">Type of state that is trying to transition.</param>
/// <param name="targetStateType">Type of state that it is attempting a transition to.</param>
/// <param name="stateMachineType">Type of state machine.</param>
public class TransitioningDuringOnLeaveIsNotSupported(Type stateType, Type targetStateType, Type stateMachineType) : Exception($"State '{stateType.AssemblyQualifiedName}' in state machine '{stateMachineType.AssemblyQualifiedName}' is trying to transition to '{targetStateType.FullName}' during OnLeave, which is not supported")
{
}
