// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Imports;

/// <summary>
/// Represents an implementation of <see cref="IImportStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> holding the collections.</param>
/// <remarks>
/// A record and a chunk are each one document carrying their serialized form beside the fields that are queried.
/// A chunk's identity is its import and chunk index, so a resent chunk collides on the key and the first one is kept.
/// </remarks>
public class ImportStorage(IEventStoreNamespaceDatabase database) : IImportStorage
{
    const string IdField = "_id";
    const string PayloadField = "payload";
    const string ImportIdField = "importId";
    const string ChunkIndexField = "chunkIndex";
    const string FirstEventIndexField = "firstEventIndex";
    const string EventsReceivedField = "eventsReceived";
    const string EventsStagedField = "eventsStaged";
    const string EventsRejectedField = "eventsRejected";
    const string StateField = "state";

    readonly IMongoCollection<BsonDocument> _records = database.GetCollection<BsonDocument>(WellKnownCollectionNames.Imports);
    readonly IMongoCollection<BsonDocument> _chunks = database.GetCollection<BsonDocument>(WellKnownCollectionNames.ImportChunks);
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public async Task<ImportRecord?> Get(ImportId importId)
    {
        using var cursor = await _records.FindAsync(Builders<BsonDocument>.Filter.Eq(IdField, Key(importId))).ConfigureAwait(false);
        var document = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        return document is null ? null : ImportStorageSerialization.DeserializeRecord(document[PayloadField].AsString);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ImportRecord>> GetAll()
    {
        using var cursor = await _records.FindAsync(Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);
        var documents = await cursor.ToListAsync().ConfigureAwait(false);
        return documents.Select(_ => ImportStorageSerialization.DeserializeRecord(_[PayloadField].AsString)).ToArray();
    }

    /// <inheritdoc/>
    public Task Save(ImportRecord record) =>
        _records.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq(IdField, Key(record.Id)),
            new BsonDocument
            {
                { IdField, Key(record.Id) },
                { StateField, (int)record.State },
                { PayloadField, ImportStorageSerialization.Serialize(record) }
            },
            new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public async Task<bool> AddChunk(StagedImportChunk chunk)
    {
        await EnsureIndexes().ConfigureAwait(false);
        var summary = chunk.ToSummary();
        try
        {
            await _chunks.InsertOneAsync(new BsonDocument
            {
                { IdField, ChunkKey(chunk.ImportId, chunk.ChunkIndex) },
                { ImportIdField, Key(chunk.ImportId) },
                { ChunkIndexField, chunk.ChunkIndex },
                { FirstEventIndexField, chunk.FirstEventIndex },
                { EventsReceivedField, summary.EventsReceived },
                { EventsStagedField, summary.EventsStaged },
                { EventsRejectedField, summary.EventsRejected },
                { PayloadField, ImportStorageSerialization.Serialize(chunk) }
            }).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<StagedImportChunk?> GetChunk(ImportId importId, int chunkIndex)
    {
        using var cursor = await _chunks.FindAsync(Builders<BsonDocument>.Filter.Eq(IdField, ChunkKey(importId, chunkIndex))).ConfigureAwait(false);
        var document = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        return document is null ? null : ImportStorageSerialization.DeserializeChunk(document[PayloadField].AsString);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ImportChunkSummary>> GetChunkSummaries(ImportId importId)
    {
        await EnsureIndexes().ConfigureAwait(false);
        var options = new FindOptions<BsonDocument>
        {
            Projection = Builders<BsonDocument>.Projection.Exclude(PayloadField),
            Sort = Builders<BsonDocument>.Sort.Ascending(ChunkIndexField)
        };
        using var cursor = await _chunks.FindAsync(Builders<BsonDocument>.Filter.Eq(ImportIdField, Key(importId)), options).ConfigureAwait(false);
        var documents = await cursor.ToListAsync().ConfigureAwait(false);
        return documents.Select(_ => new ImportChunkSummary(
            _[ChunkIndexField].ToInt32(),
            _[FirstEventIndexField].ToInt64(),
            _[EventsReceivedField].ToInt32(),
            _[EventsStagedField].ToInt32(),
            _[EventsRejectedField].ToInt32())).ToArray();
    }

    /// <inheritdoc/>
    public Task RemoveChunks(ImportId importId) =>
        _chunks.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq(ImportIdField, Key(importId)));

    /// <inheritdoc/>
    public async Task Remove(ImportId importId)
    {
        await RemoveChunks(importId).ConfigureAwait(false);
        await _records.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq(IdField, Key(importId))).ConfigureAwait(false);
    }

    static string Key(ImportId importId) => importId.Value.ToString("D", CultureInfo.InvariantCulture);

    static string ChunkKey(ImportId importId, int chunkIndex) => $"{Key(importId)}/{chunkIndex.ToString(CultureInfo.InvariantCulture)}";

    Task EnsureIndexes() => _chunks.EnsureIndexesOnceAsync(
        _ensuredIndexes,
        [new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending(ImportIdField).Ascending(ChunkIndexField),
            new CreateIndexOptions { Name = "importId_chunkIndex", Background = true })]);
}
