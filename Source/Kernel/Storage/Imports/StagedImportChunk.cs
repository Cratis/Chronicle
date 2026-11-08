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
        Rejections.Count());

    /// <summary>
    /// Tells what became of this chunk when another chunk already held its import and index, so it was not stored.
    /// </summary>
    /// <param name="staged">The summary of the chunk that holds the key, or null when it was removed before it could be read.</param>
    /// <returns><see cref="StagedChunkAddition.AlreadyStaged"/> when the staged chunk covers the same events; otherwise <see cref="StagedChunkAddition.Conflicting"/>.</returns>
    /// <remarks>
    /// Every provider answers a key collision through this, so they agree. A chunk that held the key and is gone by the
    /// time it is read was removed with its import's staging; nothing is staged under the index, so the chunk is not
    /// reported as staged.
    /// </remarks>
    public StagedChunkAddition ComparedWith(ImportChunkSummary? staged) =>
        staged?.Matches(FirstEventIndex, EventsReceived) == true ? StagedChunkAddition.AlreadyStaged : StagedChunkAddition.Conflicting;
}
