// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines;

/// <summary>
/// Represents the base state of a state machine.
/// </summary>
public class StateMachineState
{
    /// <summary>
    /// Gets or sets the current state.
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;
}
