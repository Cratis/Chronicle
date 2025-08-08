// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Auditing;
using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the context in which an event exists - typically what it was appended with.
/// </summary>
/// <param name="EventType">The type of the event.</param>
/// <param name="EventSourceType">The type of the event source.</param>
/// <param name="EventSourceId">The id of the event source.</param>
/// <param name="SequenceNumber">The sequence number of the event as persisted in the event sequence.</param>
/// <param name="EventStreamType">The type of the event stream.</param>
/// <param name="EventStreamId">The id of the event stream.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation id for the event.</param>
/// <param name="Causation">A collection of causation for what caused the event.</param>
/// <param name="CausedBy">A collection of Identities that caused the event.</param>
public record EventContext(
    EventType EventType,
    string EventSourceType,
    string EventSourceId,
    ulong SequenceNumber,
    string EventStreamType,
    string EventStreamId,
    DateTimeOffset Occurred,
    Guid CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
