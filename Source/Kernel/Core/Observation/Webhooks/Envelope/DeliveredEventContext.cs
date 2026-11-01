// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The public context of a delivered event, without storage metadata.
/// </summary>
/// <param name="SequenceNumber">The persisted sequence number.</param>
/// <param name="EventType">The delivered event type and generation.</param>
/// <param name="EventSourceType">The source type.</param>
/// <param name="EventSourceId">The source identifier.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="Causation">The causation chain.</param>
/// <param name="CausedBy">The responsible identity.</param>
/// <param name="Tags">The event tags.</param>
/// <param name="Subject">The compliance subject.</param>
/// <param name="ObservationState">The observation state.</param>
/// <param name="DedupKey">The canonical deduplication key.</param>
public record DeliveredEventContext(
    [property: JsonPropertyName("sequenceNumber")] ulong SequenceNumber,
    [property: JsonPropertyName("eventType")] DeliveredEventType EventType,
    [property: JsonPropertyName("eventSourceType")] EventSourceType EventSourceType,
    [property: JsonPropertyName("eventSourceId")] EventSourceId EventSourceId,
    [property: JsonPropertyName("eventStreamType")] EventStreamType EventStreamType,
    [property: JsonPropertyName("eventStreamId")] EventStreamId EventStreamId,
    [property: JsonPropertyName("occurred")] DateTimeOffset Occurred,
    [property: JsonPropertyName("correlationId")] CorrelationId CorrelationId,
    [property: JsonPropertyName("causation")] IEnumerable<Causation> Causation,
    [property: JsonPropertyName("causedBy")] Identity CausedBy,
    [property: JsonPropertyName("tags")] IEnumerable<Tag> Tags,
    [property: JsonPropertyName("subject")] Subject Subject,
    [property: JsonPropertyName("observationState")] EventObservationState ObservationState,
    [property: JsonPropertyName("dedupKey")] EventDedupKey DedupKey);
