// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An event delivered over a federation link.
/// </summary>
/// <param name="SequenceNumber">The sequence number in the producer's outbox.</param>
/// <param name="EventType">The event type id.</param>
/// <param name="Generation">The event type generation.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventSourceId">The event source id.</param>
/// <param name="EventStreamType">The event stream type.</param>
/// <param name="EventStreamId">The event stream id.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation id.</param>
/// <param name="Subject">The subject, empty when none.</param>
/// <param name="Content">The event content as JSON.</param>
/// <param name="Tags">The tags.</param>
public record FederatedEvent(
    ulong SequenceNumber,
    string EventType,
    uint Generation,
    string EventSourceType,
    string EventSourceId,
    string EventStreamType,
    string EventStreamId,
    DateTimeOffset Occurred,
    string CorrelationId,
    string Subject,
    string Content,
    IEnumerable<string> Tags);
