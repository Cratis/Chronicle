// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Convenience attribute that pins an observer (Reactor, Reducer, or model-bound Projection) to the
/// default event log sequence (<see cref="EventSequenceId.Log"/>).
/// </summary>
/// <remarks>
/// Equivalent to <c>[EventSequence(EventSequenceId.LogId)]</c> but more expressive.
/// Use this when you want to be explicit that the observer reads from the local event log
/// rather than an inbox or another sequence.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventLogAttribute() : EventSequenceAttribute(EventSequenceId.LogId);
