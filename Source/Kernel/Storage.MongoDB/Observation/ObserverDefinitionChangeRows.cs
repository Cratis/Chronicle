// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Driver;

using RowEncoding = Cratis.Chronicle.Storage.Observation.ObserverDefinitionChangeRows;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Stores one bounded row family with server-side conditional updates and primary reads.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public class ObserverDefinitionChangeRows<T> : IObserverDefinitionChangeRows<T>
{
    readonly IMongoCollection<BsonDocument> _rows;
    readonly Lazy<Task> _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverDefinitionChangeRows{T}"/> class.
    /// </summary>
    /// <param name="database">The namespace database.</param>
    public ObserverDefinitionChangeRows(IEventStoreNamespaceDatabase database)
    {
        _rows = database.GetCollection<BsonDocument>($"observer-definition-change-{typeof(T).Name}")
            .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).WithWriteConcern(WriteConcern.WMajority);
        _index = new(() => _rows.Indexes.CreateManyAsync(
        [
            new(new BsonDocument { { "observer", 1 }, { "revision", 1 }, { "_id", 1 } }),
            new(new BsonDocument { { "observer", 1 }, { "_id", 1 } })
        ]));
    }

    /// <inheritdoc/>
    public async Task<ObserverDefinitionChangeRow<T>?> Get(ObserverDefinitionChangeRowKey key)
    {
        var document = await _rows.Find(new BsonDocument("_id", RowEncoding.Id<T>(key))).SingleOrDefaultAsync();
        return document is null ? null : RowEncoding.Deserialize<T>(document["data"].AsString);
    }

    /// <inheritdoc/>
    public async Task<bool> TryInsert(ObserverDefinitionChangeRow<T> row)
    {
        if (row.Version != 0 || !RowEncoding.IsValid(row)) return false;
        await _index.Value;
        var document = Values(row)
            .Add("_id", RowEncoding.Id<T>(row.Key))
            .Add("observer", RowEncoding.Observer(row.Key.Observer))
            .Add("revision", row.Key.Revision.Value);
        try
        {
            await _rows.InsertOneAsync(document);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryReplace(ObserverDefinitionChangeRow<T> row, long expectedVersion)
    {
        if (!RowEncoding.IsReplacement(row, expectedVersion)) return false;
        var filter = new BsonDocument
        {
            { "_id", RowEncoding.Id<T>(row.Key) },
            { "version", expectedVersion },
            { "state", new BsonDocument { { "$lt", (int)ObserverDefinitionChangeRowState.Terminal }, { "$lte", (int)row.State } } },
            { "$or", new BsonArray
                {
                    new BsonDocument("position", BsonNull.Value),
                    new BsonDocument("position", new BsonDocument("$lte", Position(row)))
                }
            }
        };
        var result = await _rows.UpdateOneAsync(filter, new BsonDocument("$set", Values(row)));

        return result.ModifiedCount == 1;
    }

    /// <inheritdoc/>
    public async Task<ObserverDefinitionChangePage<T>> ReadPage(ObserverKey observer, ObserverTypeSetRevision? revision, string? after, int limit)
    {
        if (limit <= 0) return new([], null);
        var filter = new BsonDocument("observer", RowEncoding.Observer(observer));
        if (revision is not null) filter.Add("revision", revision.Value);
        if (after is not null) filter.Add("_id", new BsonDocument("$gt", after));
        var documents = await _rows.Find(filter).Sort(new BsonDocument("_id", 1)).Limit(Math.Min(limit, 1000)).ToListAsync();

        return new(documents.Select(document => RowEncoding.Deserialize<T>(document["data"].AsString)).ToArray(), documents.Count == 0 ? null : documents[^1]["_id"].AsString);
    }

    /// <inheritdoc/>
    public async Task<bool> TryDelete(ObserverDefinitionChangeRowKey key, long expectedVersion)
    {
        var result = await _rows.DeleteOneAsync(new BsonDocument { { "_id", RowEncoding.Id<T>(key) }, { "version", expectedVersion } });
        return result.DeletedCount == 1;
    }

    static BsonValue Position(ObserverDefinitionChangeRow<T> row) => row.Position is null ? BsonNull.Value : new BsonDecimal128(row.Position.Value);

    static BsonDocument Values(ObserverDefinitionChangeRow<T> row) => new()
    {
        { "version", row.Version },
        { "state", (int)row.State },
        { "position", Position(row) },
        { "data", RowEncoding.Serialize(row) }
    };
}
