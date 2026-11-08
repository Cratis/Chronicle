// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the result of staging one chunk of an import.
/// </summary>
/// <param name="ChunkIndex">The index of the chunk.</param>
/// <param name="AlreadyStaged">True when the chunk had been staged before and the earlier result is returned.</param>
/// <param name="EventsReceived">How many events the chunk carried.</param>
/// <param name="EventsStaged">How many of them were staged.</param>
/// <param name="Rejections">The events that were rejected, by their index in the import.</param>
/// <param name="Error">Why the chunk was refused as a whole, when it was; nothing of it was staged then.</param>
public record ImportChunkStagingResult(
    int ChunkIndex,
    bool AlreadyStaged,
    int EventsReceived,
    int EventsStaged,
    IEnumerable<ImportRejection> Rejections,
    string? Error = default)
{
    /// <summary>
    /// Creates the result for a chunk refused as a whole.
    /// </summary>
    /// <param name="chunkIndex">The index of the chunk.</param>
    /// <param name="error">Why.</param>
    /// <returns>The <see cref="ImportChunkStagingResult"/>.</returns>
    public static ImportChunkStagingResult Refused(int chunkIndex, string error) => new(chunkIndex, false, 0, 0, [], error);

    /// <summary>
    /// Creates the result for a staged chunk.
    /// </summary>
    /// <param name="chunk">The <see cref="StagedImportChunk"/>.</param>
    /// <param name="alreadyStaged">Whether the chunk was staged before.</param>
    /// <returns>The <see cref="ImportChunkStagingResult"/>.</returns>
    public static ImportChunkStagingResult From(StagedImportChunk chunk, bool alreadyStaged) =>
        new(chunk.ChunkIndex, alreadyStaged, chunk.EventsReceived, chunk.Events.Count(), chunk.Rejections.ToArray());
}
