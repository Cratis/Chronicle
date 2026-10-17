// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

/// <summary>
/// Stores first-writer-wins lifecycle snapshots in the event-store database.
/// </summary>
/// <param name="database">The event-store database.</param>
public class LifecycleFanOutStorage(IMongoDatabase database) : ILifecycleFanOutStorage
{
    readonly IMongoCollection<BsonDocument> _intents = database.GetCollection<BsonDocument>("lifecycle-fan-out-intents");

    /// <inheritdoc/>
    public async Task<LifecycleFanOutIntent?> Get(OccurrenceId id)
    {
        var document = await _intents.Find(new BsonDocument("_id", id.Value)).SingleOrDefaultAsync();
        return document is null ? null : FromDocument(document);
    }

    /// <inheritdoc/>
    public async Task<bool> TryInsert(LifecycleFanOutIntent intent)
    {
        try
        {
            await _intents.InsertOneAsync(new BsonDocument
            {
                { "_id", intent.Occurrence.Id.Value },
                { "data", JsonSerializer.Serialize(intent) },
                { "progress", intent.Progress },
                { "count", intent.Namespaces.Count }
            });
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LifecycleFanOutIntent>> GetPending(int max)
    {
        if (max <= 0) return [];
        var filter = new BsonDocument("$expr", new BsonDocument("$lt", new BsonArray { "$progress", "$count" }));
        var documents = await _intents.Find(filter).Sort(new BsonDocument("_id", 1)).Limit(Math.Min(max, 100)).ToListAsync();
        return documents.Select(FromDocument).ToArray();
    }

    /// <inheritdoc/>
    public async Task Advance(OccurrenceId id, int progress)
    {
        var filter = new BsonDocument { { "_id", id.Value }, { "count", new BsonDocument("$gte", progress) } };
        await _intents.UpdateOneAsync(filter, new BsonDocument("$max", new BsonDocument("progress", progress)));
    }

    static LifecycleFanOutIntent FromDocument(BsonDocument document) => JsonSerializer.Deserialize<LifecycleFanOutIntent>(document["data"].AsString)! with { Progress = document["progress"].AsInt32 };
}
