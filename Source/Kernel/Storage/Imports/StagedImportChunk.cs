// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents one chunk of an import held in staging.
/// </summary>
/// <param name="ImportId">The import the chunk belongs to.</param>
/// <param name="ChunkIndex">The index of the chunk, unique within the import.</param>
/// <param name="FirstEventIndex">The index in the import of the first event in the chunk.</param>
/// <param name="EventsReceived">How many events the chunk carried, staged or rejected.</param>
/// <param name="Events">The staged events, in the order received.</param>
/// <param name="Rejections">The events of the chunk that were rejected while staging.</param>
public record StagedImportChunk(
    ImportId ImportId,
    int ChunkIndex,
    long FirstEventIndex,
    int EventsReceived,
    IEnumerable<StagedImportEvent> Events,
    IEnumerable<ImportRejection> Rejections)
{
    /// <summary>
    /// Gets the summary of the chunk.
    /// </summary>
    /// <returns>The <see cref="ImportChunkSummary"/>.</returns>
    public ImportChunkSummary ToSummary() => new(
        ChunkIndex,
        FirstEventIndex,
        EventsReceived,
        Events.Count(),
        Rejections.Count(),
        [.. Events.Select(_ => EventTypeIdOf(_.EventType)).Distinct(StringComparer.Ordinal)]);

    /// <summary>
    /// Gets the event type identifier of a staged event type, which is written as its identifier and generation.
    /// </summary>
    /// <param name="eventType">The staged event type.</param>
    /// <returns>The event type identifier.</returns>
    public static string EventTypeIdOf(string eventType) => eventType.Split('+')[0];
}
