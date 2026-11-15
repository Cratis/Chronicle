// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Turns staged import events into what the event sequence appends, recording the import in their causation.
/// </summary>
internal static class StagedImportEventConverter
{
    /// <summary>
    /// Converts the events of a staged chunk.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <param name="chunkIndex">The index of the chunk.</param>
    /// <param name="events">The staged events.</param>
    /// <param name="occurred">When the import causation is recorded.</param>
    /// <returns>The <see cref="ImportEventToAppend">events to append</see>.</returns>
    internal static IReadOnlyList<ImportEventToAppend> ToEventsToAppend(ImportId importId, int chunkIndex, IEnumerable<StagedImportEvent> events, DateTimeOffset occurred) =>
        events.Select(_ => ToEventToAppend(importId, chunkIndex, _, occurred)).ToArray();

    /// <summary>
    /// Converts one staged event.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <param name="chunkIndex">The index of the chunk.</param>
    /// <param name="staged">The <see cref="StagedImportEvent"/>.</param>
    /// <param name="occurred">When the import causation is recorded.</param>
    /// <returns>The <see cref="ImportEventToAppend"/>.</returns>
    internal static ImportEventToAppend ToEventToAppend(ImportId importId, int chunkIndex, StagedImportEvent staged, DateTimeOffset occurred)
    {
        var properties = new Dictionary<string, string>
        {
            [EventSequence.ImportIdProperty] = importId.Value.ToString("D", CultureInfo.InvariantCulture),
            [EventSequence.ImportChunkIndexProperty] = chunkIndex.ToString(CultureInfo.InvariantCulture),
            [EventSequence.ImportEventIndexProperty] = staged.Index.ToString(CultureInfo.InvariantCulture)
        };
        if (staged.SourceSequenceNumber is { } source)
            properties[EventSequence.ImportSourceSequenceNumberProperty] = source.ToString(CultureInfo.InvariantCulture);

        // The import is the root of every imported event's causation; the source's own chain follows it unchanged.
        IEnumerable<Causation> causation = [new Causation(occurred, CausationType.Import, properties), .. staged.Causation ?? []];

        var @event = new EventToAppend(
            staged.EventSourceType,
            staged.EventSourceId,
            staged.EventStreamType,
            staged.EventStreamId,
            EventType.Parse(staged.EventType),
            staged.Tags.Select(_ => new Tag(_)).ToArray(),
            JsonNode.Parse(staged.Content)!.AsObject(),
            staged.Occurred,
            staged.Subject is null ? null : new Subject(staged.Subject))
        {
            NamedTags = staged.NamedTags.Select(_ => new NamedTag(_.Name, _.Value)).ToArray(),
            EventSource = staged.EventSource is null ? null : new EventSourceName(staged.EventSource),
            Causation = causation
        };

        return new(
            staged.Index,
            @event,
            staged.IdempotencyKey is null ? null : new IdempotencyKey(staged.IdempotencyKey),
            staged.CorrelationId is { } correlation ? new CorrelationId(correlation) : null,
            staged.CausedBy);
    }
}
