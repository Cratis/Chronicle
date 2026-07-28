// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// The exception that is thrown by a substituted grain storage to simulate the warm-start state snapshot failing
/// after a durable append.
/// </summary>
public class SimulatedStateWriteError() : Exception("Simulated failure writing the event sequence state");
