// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations;

/// <summary>
/// Stores receipts and blocker audit records atomically in separate keyed documents.
/// </summary>
/// <param name="collection">The store database.</param>
public sealed class GenerationInitializationStorage(IMongoCollection<BsonDocument> collection) : IGenerationInitializationStorage
{
    readonly IMongoCollection<BsonDocument> _collection = collection;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationInitializationReceipt>> GetReceipts() =>
        (await _collection.Find(Builders<BsonDocument>.Filter.Exists("receipt")).ToListAsync())
            .Select(_ => BsonSerializer.Deserialize<GenerationInitializationReceipt>(_["receipt"].AsBsonDocument)).ToArray();

    /// <inheritdoc/>
    public async Task<bool> TryRecord(GenerationInitializationReceipt receipt)
    {
        try
        {
            await _collection.InsertOneAsync(new BsonDocument { { "_id", $"{receipt.Step}/{receipt.Subject}" }, { "receipt", receipt.ToBsonDocument() } });
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationInitializationBlocker>> GetBlockers() =>
        (await _collection.Find(Builders<BsonDocument>.Filter.Exists("blocker")).ToListAsync())
            .Select(_ => BsonSerializer.Deserialize<GenerationInitializationBlocker>(_["blocker"].AsBsonDocument)).ToArray();

    /// <inheritdoc/>
    public async Task RecordBlocker(GenerationInitializationBlocker blocker)
    {
        var key = $"blocker/{blocker.Kind}/{blocker.Subject}";
        while (true)
        {
            var existing = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", key)).FirstOrDefaultAsync();
            var next = blocker;
            if (existing is not null)
            {
                var old = BsonSerializer.Deserialize<GenerationInitializationBlocker>(existing["blocker"].AsBsonDocument);
                next = blocker with { RaisedAt = old.ResolvedAt is null ? old.RaisedAt : blocker.RaisedAt };
                var filter = Builders<BsonDocument>.Filter.Eq("_id", key) & Builders<BsonDocument>.Filter.Eq("blocker", existing["blocker"]);
                var result = await _collection.ReplaceOneAsync(filter, new BsonDocument { { "_id", key }, { "blocker", next.ToBsonDocument() } });
                if (result.MatchedCount == 1)
                {
                    return;
                }
                continue;
            }
            try
            {
                await _collection.InsertOneAsync(new BsonDocument { { "_id", key }, { "blocker", next.ToBsonDocument() } });
                return;
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Re-read the winning audit record instead of replacing its raised time.
            }
        }
    }

    /// <inheritdoc/>
    public async Task ResolveBlocker(GenerationInitializationBlockerKind kind, string subject, DateTimeOffset at)
    {
        var key = $"blocker/{kind}/{subject}";
        var existing = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", key)).FirstOrDefaultAsync();
        if (existing is null)
        {
            return;
        }
        var blocker = BsonSerializer.Deserialize<GenerationInitializationBlocker>(existing["blocker"].AsBsonDocument);
        if (blocker.ResolvedAt is null)
        {
            await _collection.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", key) & Builders<BsonDocument>.Filter.Eq("blocker", existing["blocker"]),
                new BsonDocument { { "_id", key }, { "blocker", (blocker with { ResolvedAt = at }).ToBsonDocument() } });
        }
    }
}
