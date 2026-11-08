// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.InMemory.Imports;

/// <summary>
/// Represents an in-memory implementation of <see cref="IImportStorage"/>.
/// </summary>
/// <remarks>
/// Records and chunks are kept as their serialized form, exactly as the persistent providers keep them, so a caller
/// never shares an instance with the store and every value round-trips through the same serialization.
/// </remarks>
public sealed class ImportStorage : IImportStorage
{
    readonly ConcurrentDictionary<ImportId, string> _records = new();
    readonly ConcurrentDictionary<(ImportId ImportId, int ChunkIndex), (ImportChunkSummary Summary, string Json)> _chunks = new();

    /// <inheritdoc/>
    public Task<ImportRecord?> Get(ImportId importId) =>
        Task.FromResult(_records.TryGetValue(importId, out var json) ? ImportStorageSerialization.DeserializeRecord(json) : null);

    /// <inheritdoc/>
    public Task<IEnumerable<ImportRecord>> GetAll() =>
        Task.FromResult<IEnumerable<ImportRecord>>(_records.Values.Select(ImportStorageSerialization.DeserializeRecord).ToArray());

    /// <inheritdoc/>
    public Task Save(ImportRecord record)
    {
        _records[record.Id] = ImportStorageSerialization.Serialize(record);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<StagedChunkAddition> AddChunk(StagedImportChunk chunk)
    {
        var summary = chunk.ToSummary();
        var stored = _chunks.GetOrAdd((chunk.ImportId, chunk.ChunkIndex), static (_, argument) => (argument.Summary, ImportStorageSerialization.Serialize(argument.Chunk)), (Summary: summary, Chunk: chunk));
        if (ReferenceEquals(stored.Summary, summary)) return Task.FromResult(StagedChunkAddition.Added);
        return Task.FromResult(chunk.ComparedWith(stored.Summary));
    }

    /// <inheritdoc/>
    public Task<StagedImportChunk?> GetChunk(ImportId importId, int chunkIndex) =>
        Task.FromResult(_chunks.TryGetValue((importId, chunkIndex), out var chunk) ? ImportStorageSerialization.DeserializeChunk(chunk.Json) : null);

    /// <inheritdoc/>
    public Task<IEnumerable<ImportChunkSummary>> GetChunkSummaries(ImportId importId) =>
        Task.FromResult<IEnumerable<ImportChunkSummary>>(_chunks
            .Where(_ => _.Key.ImportId == importId)
            .Select(_ => _.Value.Summary)
            .OrderBy(_ => _.ChunkIndex)
            .ToArray());

    /// <inheritdoc/>
    public Task RemoveChunk(ImportId importId, int chunkIndex)
    {
        _chunks.TryRemove((importId, chunkIndex), out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveChunks(ImportId importId)
    {
        foreach (var key in _chunks.Keys.Where(_ => _.ImportId == importId).ToArray()) _chunks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Remove(ImportId importId)
    {
        await RemoveChunks(importId);
        _records.TryRemove(importId, out _);
    }
}
