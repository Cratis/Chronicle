// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Tracks distinct destination documents within one transaction, including keyed upserts.
/// </summary>
/// <param name="cap">The maximum number of documents.</param>
internal sealed class SinkBatchDocuments(int cap)
{
    readonly HashSet<(string Collection, BsonValue Id)> _documents = [];

    /// <summary>
    /// Reads destination keys, bounded when reserving a single atomic batch.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="session">The optional active transaction.</param>
    /// <param name="filter">The destination filter.</param>
    /// <param name="limit">The optional maximum number of keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The destination keys.</returns>
    internal static async Task<IReadOnlyList<BsonValue>> FindIds(IMongoCollection<BsonDocument> collection, IClientSessionHandle? session, FilterDefinition<BsonDocument> filter, int? limit, CancellationToken cancellationToken)
    {
        var options = new FindOptions<BsonDocument, BsonDocument> { Projection = Builders<BsonDocument>.Projection.Include("_id"), Limit = limit };
        using var cursor = session is null
            ? await collection.FindAsync(filter, options, cancellationToken)
            : await collection.FindAsync(session, filter, options, cancellationToken);
        return (await cursor.ToListAsync(cancellationToken)).ConvertAll(document => document["_id"]);
    }

    /// <summary>
    /// Refuses updates that cannot be replayed after a partially committed changeset.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="update">The update.</param>
    /// <exception cref="InvalidOperationException">The update is not idempotent.</exception>
    internal static void RequireRetrySafe(IMongoCollection<BsonDocument> collection, UpdateDefinition<BsonDocument> update)
    {
        var rendered = update.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
        if (rendered is not BsonDocument document || document.ElementCount == 0 ||
            document.Names.Any(name => name is not ("$set" or "$unset" or "$max" or "$min" or "$setOnInsert" or "$pull")))
        {
            throw new InvalidOperationException("A split fenced changeset requires idempotent set/upsert/delete writes; this update cannot be safely retried.");
        }
    }

    /// <summary>
    /// Refuses bulk operations that cannot be replayed after partial progress.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="requests">The ordered requests.</param>
    /// <exception cref="InvalidOperationException">A request is not retry-safe.</exception>
    internal static void RequireRetrySafe(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests)
    {
        foreach (var request in requests)
        {
            switch (request)
            {
                case UpdateOneModel<BsonDocument> update:
                    RequireRetrySafe(collection, update.Update);
                    RequireDestinationKey(collection, update.Filter);
                    break;
                case DeleteOneModel<BsonDocument> delete:
                    RequireDestinationKey(collection, delete.Filter);
                    break;
                case ReplaceOneModel<BsonDocument> replace:
                    RequireDestinationKey(collection, replace.Filter);
                    break;
                default:
                    throw new InvalidOperationException("A split fenced bulk write requires idempotent keyed update, replacement or delete requests.");
            }
        }
    }

    /// <summary>
    /// Refuses a single-document split write without a stable destination key.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="filter">The destination filter.</param>
    /// <exception cref="InvalidOperationException">The filter does not name a key.</exception>
    internal static void RequireDestinationKey(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter)
    {
        if (Key(filter.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry))) is null)
        {
            throw new InvalidOperationException("A split fenced single-document write requires an equality filter on _id.");
        }
    }

    /// <summary>
    /// Reserves the destination of one bulk request before it is written.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="session">The active transaction.</param>
    /// <param name="request">The write request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The request is not a single-document operation.</exception>
    internal async Task Reserve(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, WriteModel<BsonDocument> request, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case InsertOneModel<BsonDocument> insert:
                Add(collection, insert.Document["_id"]);
                break;
            case UpdateOneModel<BsonDocument> update:
                await Reserve(collection, session, update.Filter, false, update.IsUpsert, cancellationToken);
                break;
            case ReplaceOneModel<BsonDocument> replace:
                await Reserve(collection, session, replace.Filter, false, replace.IsUpsert, cancellationToken);
                break;
            case DeleteOneModel<BsonDocument> delete:
                await Reserve(collection, session, delete.Filter, false, false, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("A fenced sink bulk write requires single-document requests.");
        }
    }

    /// <summary>
    /// Reserves distinct destination keys before admitting a write to the batch.
    /// </summary>
    /// <param name="collection">The destination collection.</param>
    /// <param name="session">The active transaction.</param>
    /// <param name="filter">The destination filter.</param>
    /// <param name="many">Whether the operation targets multiple documents.</param>
    /// <param name="upsert">Whether a missing destination will be inserted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="InvalidOperationException">An upsert does not name its destination key.</exception>
    internal async Task Reserve(IMongoCollection<BsonDocument> collection, IClientSessionHandle session, FilterDefinition<BsonDocument> filter, bool many, bool upsert, CancellationToken cancellationToken)
    {
        var ids = await FindIds(collection, session, filter, many ? (int)Math.Min((long)cap + 1, int.MaxValue) : 1, cancellationToken);
        foreach (var id in ids)
        {
            Add(collection, id);
        }
        if (ids.Count == 0 && upsert)
        {
            var rendered = filter.Render(new(collection.DocumentSerializer, collection.Settings.SerializerRegistry));
            Add(collection, Key(rendered) ?? throw new InvalidOperationException("A fenced sink upsert requires an equality filter on _id."));
        }
    }

    static BsonValue? Key(BsonDocument filter)
    {
        if (filter.TryGetValue("_id", out var id))
        {
            return id is BsonDocument condition && condition.ElementCount > 0 && condition.GetElement(0).Name.StartsWith('$')
                ? condition.GetValue("$eq", null)
                : id;
        }
        return filter.TryGetValue("$and", out var clauses)
            ? clauses.AsBsonArray.Select(clause => Key(clause.AsBsonDocument)).FirstOrDefault(id => id is not null)
            : null;
    }

    void Add(IMongoCollection<BsonDocument> collection, BsonValue id)
    {
        _documents.Add((collection.CollectionNamespace.FullName, id));
        if (_documents.Count > cap)
        {
            throw new SinkBatchLimitExceeded();
        }
    }
}
