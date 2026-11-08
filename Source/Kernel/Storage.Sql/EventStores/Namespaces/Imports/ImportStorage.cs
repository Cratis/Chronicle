// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports;

/// <summary>
/// Represents an implementation of <see cref="IImportStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use.</param>
/// <remarks>
/// A chunk's primary key is its import and chunk index, so a resent chunk is refused by the key and the first one kept.
/// </remarks>
public class ImportStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IImportStorage
{
    /// <inheritdoc/>
    public async Task<ImportRecord?> Get(ImportId importId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        var entity = await scope.DbContext.Imports.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == id);
        return entity is null ? null : ImportStorageSerialization.DeserializeRecord(entity.Payload);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ImportRecord>> GetAll()
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var entities = await scope.DbContext.Imports.AsNoTracking().ToListAsync();
        return entities.Select(_ => ImportStorageSerialization.DeserializeRecord(_.Payload)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(ImportRecord record)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = record.Id.Value;
        var entity = await scope.DbContext.Imports.FirstOrDefaultAsync(_ => _.Id == id);
        if (entity is null)
        {
            entity = new ImportEntity { Id = id };
            scope.DbContext.Imports.Add(entity);
        }

        entity.State = (int)record.State;
        entity.Payload = ImportStorageSerialization.Serialize(record);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<StagedChunkAddition> AddChunk(StagedImportChunk chunk)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var importId = chunk.ImportId.Value;
        var chunkIndex = chunk.ChunkIndex;
        if (await Existing(scope.DbContext, importId, chunkIndex) is { } existing) return Compare(existing, chunk);

        var summary = chunk.ToSummary();
        scope.DbContext.ImportChunks.Add(new ImportChunkEntity
        {
            ImportId = importId,
            ChunkIndex = chunkIndex,
            FirstEventIndex = chunk.FirstEventIndex,
            EventsReceived = summary.EventsReceived,
            EventsStaged = summary.EventsStaged,
            EventsRejected = summary.EventsRejected,
            EventTypes = JsonSerializer.Serialize(summary.EventTypes, ImportStorageSerialization.Options),
            Payload = ImportStorageSerialization.Serialize(chunk)
        });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return StagedChunkAddition.Added;
        }
        catch (DbUpdateException)
        {
            // A concurrent resend of the same chunk won the key. Anything else is a real failure.
            await using var check = await database.Namespace(eventStore, @namespace);
            if (await Existing(check.DbContext, importId, chunkIndex) is { } winner) return Compare(winner, chunk);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<StagedImportChunk?> GetChunk(ImportId importId, int chunkIndex)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        var entity = await scope.DbContext.ImportChunks.AsNoTracking().FirstOrDefaultAsync(_ => _.ImportId == id && _.ChunkIndex == chunkIndex);
        return entity is null ? null : ImportStorageSerialization.DeserializeChunk(entity.Payload);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ImportChunkSummary>> GetChunkSummaries(ImportId importId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        var rows = await scope.DbContext.ImportChunks.AsNoTracking()
            .Where(_ => _.ImportId == id)
            .OrderBy(_ => _.ChunkIndex)
            .Select(_ => new { _.ChunkIndex, _.FirstEventIndex, _.EventsReceived, _.EventsStaged, _.EventsRejected, _.EventTypes })
            .ToListAsync();
        return rows.Select(_ => new ImportChunkSummary(_.ChunkIndex, _.FirstEventIndex, _.EventsReceived, _.EventsStaged, _.EventsRejected, EventTypesOf(_.EventTypes))).ToArray();
    }

    /// <inheritdoc/>
    public async Task RemoveChunk(ImportId importId, int chunkIndex)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        await scope.DbContext.ImportChunks.Where(_ => _.ImportId == id && _.ChunkIndex == chunkIndex).ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task RemoveChunks(ImportId importId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        await scope.DbContext.ImportChunks.Where(_ => _.ImportId == id).ExecuteDeleteAsync();
    }

    /// <inheritdoc/>
    public async Task Remove(ImportId importId)
    {
        await RemoveChunks(importId);
        await using var scope = await database.Namespace(eventStore, @namespace);
        var id = importId.Value;
        await scope.DbContext.Imports.Where(_ => _.Id == id).ExecuteDeleteAsync();
    }

    static StagedChunkAddition Compare(ImportChunkEntity existing, StagedImportChunk chunk) =>
        existing.FirstEventIndex == chunk.FirstEventIndex && existing.EventsReceived == chunk.EventsReceived
            ? StagedChunkAddition.AlreadyStaged
            : StagedChunkAddition.Conflicting;

    static string[] EventTypesOf(string json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<string[]>(json, ImportStorageSerialization.Options) ?? [];

    static Task<ImportChunkEntity?> Existing(NamespaceDbContext dbContext, Guid importId, int chunkIndex) =>
        dbContext.ImportChunks.AsNoTracking()
            .Where(_ => _.ImportId == importId && _.ChunkIndex == chunkIndex)
            .Select(_ => new ImportChunkEntity { ImportId = _.ImportId, ChunkIndex = _.ChunkIndex, FirstEventIndex = _.FirstEventIndex, EventsReceived = _.EventsReceived })
            .FirstOrDefaultAsync();
}
