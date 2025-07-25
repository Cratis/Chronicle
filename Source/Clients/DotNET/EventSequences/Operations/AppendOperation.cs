// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Represents an operation to append an event to an event sequence.
/// </summary>
/// <param name="Event">The event to append.</param>
/// <param name="Causation">Optional The causation for the event.</param>
/// <param name="EventStreamType">Optional The type of event stream.</param>
/// <param name="EventStreamId">Optional The identifier for the event stream.</param>
/// <param name="EventSourceType">Optional The type of event source.</param>
public record AppendOperation(
    object Event,
    Causation? Causation = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default,
    EventSourceType? EventSourceType = default) : IEventSequenceOperation;
