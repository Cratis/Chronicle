// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents one event held in import staging, as received and validated for its schema, before it is committed.
/// </summary>
/// <param name="Index">The index of the event in the import.</param>
/// <param name="EventType">The event type, as its identifier and generation.</param>
/// <param name="EventSourceType">The resolved event source type.</param>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventStreamType">The event stream type.</param>
/// <param name="EventStreamId">The event stream identifier.</param>
/// <param name="Content">The content as received, as serialized JSON. Staging never encrypts.</param>
/// <param name="Tags">The tags of the event.</param>
/// <param name="NamedTags">The named tags of the event.</param>
/// <param name="Occurred">When the event occurred in the source, if the source says.</param>
/// <param name="Subject">The compliance subject, if not the event source.</param>
/// <param name="CorrelationId">The correlation identifier from the source, if any.</param>
/// <param name="Causation">The causation chain from the source, if any.</param>
/// <param name="CausedBy">The identity that caused the event in the source, if any.</param>
/// <param name="IdempotencyKey">The idempotency key, typically the source system's event identifier.</param>
/// <param name="EventSource">The registered event source definition the event is appended through, if any.</param>
/// <param name="SourceSequenceNumber">The sequence number of the event in its source, if any.</param>
public record StagedImportEvent(
    long Index,
    string EventType,
    string EventSourceType,
    string EventSourceId,
    string EventStreamType,
    string EventStreamId,
    string Content,
    IEnumerable<string> Tags,
    IEnumerable<StagedImportNamedTag> NamedTags,
    DateTimeOffset? Occurred,
    string? Subject,
    Guid? CorrelationId,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy,
    string? IdempotencyKey,
    string? EventSource,
    ulong? SourceSequenceNumber);
