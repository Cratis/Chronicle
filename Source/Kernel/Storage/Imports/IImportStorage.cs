// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Defines the staging storage for import sessions within an event store namespace.
/// </summary>
/// <remarks>
/// Every provider must give the same answers: a chunk is keyed by its import and chunk index, a second chunk with the
/// same key is never stored over the first, and summaries come back ordered by chunk index.
/// </remarks>
public interface IImportStorage
{
    /// <summary>
    /// Gets an import.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <returns>The <see cref="ImportRecord"/>, or null when there is none.</returns>
    Task<ImportRecord?> Get(ImportId importId);

    /// <summary>
    /// Gets every import in the namespace, whatever its state.
    /// </summary>
    /// <returns>The imports.</returns>
    Task<IEnumerable<ImportRecord>> GetAll();

    /// <summary>
    /// Inserts or replaces an import.
    /// </summary>
    /// <param name="record">The <see cref="ImportRecord"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(ImportRecord record);

    /// <summary>
    /// Stages a chunk unless a chunk with the same import and index is already staged.
    /// </summary>
    /// <param name="chunk">The <see cref="StagedImportChunk"/>.</param>
    /// <returns>True when the chunk was stored; false when one with the same key was already staged and was kept.</returns>
    Task<bool> AddChunk(StagedImportChunk chunk);

    /// <summary>
    /// Gets a staged chunk.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <param name="chunkIndex">The chunk index.</param>
    /// <returns>The <see cref="StagedImportChunk"/>, or null when it was never staged.</returns>
    Task<StagedImportChunk?> GetChunk(ImportId importId, int chunkIndex);

    /// <summary>
    /// Gets the summaries of every staged chunk of an import, ordered by chunk index.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <returns>The <see cref="ImportChunkSummary">summaries</see>.</returns>
    Task<IEnumerable<ImportChunkSummary>> GetChunkSummaries(ImportId importId);

    /// <summary>
    /// Removes every staged chunk of an import, keeping the import itself.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveChunks(ImportId importId);

    /// <summary>
    /// Removes an import and every staged chunk of it.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ImportId importId);
}
