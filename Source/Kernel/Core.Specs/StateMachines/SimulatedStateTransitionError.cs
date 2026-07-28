// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines;

/// <summary>
/// The exception that is thrown by a state double to simulate a state transition callback failing.
/// </summary>
public class SimulatedStateTransitionError() : Exception("Simulated failure inside a state transition callback");
