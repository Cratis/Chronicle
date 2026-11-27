// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>Maintains the singleton namespace-local conflict marker on the same database/client as protected events.</summary>
/// <param name="collection">The dedicated marker collection in the event/receipt database.</param>
public sealed class NamespaceMarkerStorage(IMongoCollection<BsonDocument> collection) : INamespaceMarkerStorage
{
    static readonly FilterDefinition<BsonDocument> _id = Builders<BsonDocument>.Filter.Eq("_id", 1);
    readonly IMongoCollection<BsonDocument> _collection = collection
        .WithWriteConcern(WriteConcern.WMajority)
        .WithReadConcern(ReadConcern.Majority)
        .WithReadPreference(ReadPreference.Primary);

    /// <inheritdoc/>
    public async Task<NamespaceMarker?> Get()
    {
        var document = await _collection.Find(_id).FirstOrDefaultAsync();
        return document is null
            ? null
            : new(
                new(document["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)),
                document["isOpen"].AsBoolean,
                document["revision"].AsInt64);
    }

    /// <inheritdoc/>
    public async Task<bool> TryOpen(NamespaceIncarnationId incarnation, NamespaceIncarnationId? previous)
    {
        if (incarnation.Value == Guid.Empty || previous?.Value == Guid.Empty) return false;
        var current = await Get();
        if (current is null) return await Insert(incarnation);
        if (current.Incarnation == incarnation && current.IsOpen) return true;
        if (current.IsOpen || previous != current.Incarnation) return false;
        var filter = Identity(current.Incarnation) & Builders<BsonDocument>.Filter.Eq("isOpen", false)
            & Builders<BsonDocument>.Filter.Eq("revision", current.Touch);
        var update = Builders<BsonDocument>.Update.Set("incarnation", Uuid(incarnation)).Set("isOpen", true).Inc("revision", 1L);
        return await Update(filter, update);
    }

    /// <inheritdoc/>
    public Task<bool> TryClose(NamespaceIncarnationId incarnation) => incarnation.Value == Guid.Empty
        ? Task.FromResult(false)
        : Update(Identity(incarnation), Builders<BsonDocument>.Update.Set("isOpen", false).Inc("revision", 1L));

    /// <summary>Writes the real conflict revision inside the caller's event/receipt transaction; never upserts.</summary>
    /// <param name="session">An active transaction on this namespace's client, with majority commit acknowledgement.</param>
    /// <param name="expected">The exact supplied incarnation.</param>
    /// <param name="cancellationToken">Cancellation for the calling activation.</param>
    /// <returns>True only when exactly one open marker was changed.</returns>
    /// <exception cref="InvalidOperationException">The caller did not start a transaction.</exception>
    public async Task<bool> TryFence(IClientSessionHandle session, NamespaceIncarnationId expected, CancellationToken cancellationToken = default)
    {
        if (expected.Value == Guid.Empty) return false;
        if (!session.IsInTransaction) throw new InvalidOperationException("A namespace fence requires an active event transaction.");
        var filter = Identity(expected) & Builders<BsonDocument>.Filter.Eq("isOpen", true);
        var result = await _collection.UpdateOneAsync(session, filter, Builders<BsonDocument>.Update.Inc("revision", 1L), new UpdateOptions { IsUpsert = false }, cancellationToken);
        return Changed(result);
    }

    /// <summary>Reads a refusal diagnostic from the same transaction snapshot as the failed fence.</summary>
    /// <param name="session">The refusing event transaction.</param>
    /// <param name="expected">The supplied identity, never replaced by this read.</param>
    /// <param name="cancellationToken">The activation cancellation.</param>
    /// <returns>A typed diagnostic without changing the marker or admitting a write.</returns>
    public async Task<NamespaceCommitRejected> GetRejection(IClientSessionHandle session, NamespaceIncarnationId expected, CancellationToken cancellationToken)
    {
        var document = await _collection.Find(session, _id).FirstOrDefaultAsync(cancellationToken);
        NamespaceMarker? marker = document is null
            ? null
            : new(
                new(document["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)),
                document["isOpen"].AsBoolean,
                document["revision"].AsInt64);
        return NamespaceCommitRejected.ForMarker(marker, expected);
    }

    static BsonBinaryData Uuid(NamespaceIncarnationId value) => new(value.Value, GuidRepresentation.Standard);

    static FilterDefinition<BsonDocument> Identity(NamespaceIncarnationId value) => _id & Builders<BsonDocument>.Filter.Eq("incarnation", Uuid(value));

    static bool Changed(UpdateResult result) => result.IsAcknowledged && result.MatchedCount == 1 && result.ModifiedCount == 1;

    async Task<bool> Insert(NamespaceIncarnationId next)
    {
        try
        {
            await _collection.InsertOneAsync(new BsonDocument { { "_id", 1 }, { "incarnation", Uuid(next) }, { "isOpen", true }, { "revision", 0L } });
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    async Task<bool> Update(FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update) =>
        Changed(await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false }));
}
