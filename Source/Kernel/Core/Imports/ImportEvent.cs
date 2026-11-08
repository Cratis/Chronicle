// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents one event of an import, carrying everything the source says about it.
/// </summary>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The stream type within the event source.</param>
/// <param name="EventStreamId">The stream within the stream type.</param>
/// <param name="EventType">The type and generation of the event.</param>
/// <param name="Content">The content of the event, as serialized JSON.</param>
/// <param name="Tags">The tags of the event.</param>
/// <param name="NamedTags">The named tags of the event.</param>
/// <param name="Occurred">When the event occurred in its source. Defaults to the time of the commit.</param>
/// <param name="Subject">The compliance subject. Defaults to the event source.</param>
/// <param name="CorrelationId">The correlation identifier from the source.</param>
/// <param name="Causation">The causation chain from the source; the import is recorded at its root.</param>
/// <param name="CausedBy">The identity that caused the event in the source.</param>
/// <param name="IdempotencyKey">The idempotency key, typically the source system's event identifier.</param>
/// <param name="EventSource">The registered event source definition the event is appended through.</param>
/// <param name="SourceSequenceNumber">The sequence number the event had in its source.</param>
public record ImportEvent(
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    string Content,
    IEnumerable<string>? Tags = default,
    IEnumerable<NamedTag>? NamedTags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default,
    Guid? CorrelationId = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    string? IdempotencyKey = default,
    string? EventSource = default,
    ulong? SourceSequenceNumber = default);
