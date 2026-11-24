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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Imports.ImportEvent")]
public record ImportEvent(
    [Id(0)] string EventSourceId,
    [Id(1)] string EventSourceType,
    [Id(2)] string EventStreamType,
    [Id(3)] string EventStreamId,
    [Id(4)] EventType EventType,
    [Id(5)] string Content,
    [Id(6)] IEnumerable<string>? Tags = default,
    [Id(7)] IEnumerable<NamedTag>? NamedTags = default,
    [Id(8)] DateTimeOffset? Occurred = default,
    [Id(9)] string? Subject = default,
    [Id(10)] Guid? CorrelationId = default,
    [Id(11)] IEnumerable<Causation>? Causation = default,
    [Id(12)] Identity? CausedBy = default,
    [Id(13)] string? IdempotencyKey = default,
    [Id(14)] string? EventSource = default,
    [Id(15)] ulong? SourceSequenceNumber = default);
