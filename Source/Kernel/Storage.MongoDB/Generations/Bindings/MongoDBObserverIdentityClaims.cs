// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>
/// Represents the MongoDB implementation of <see cref="IObserverIdentityClaims"/>, one single-document write per claim.
/// </summary>
/// <param name="database">The database resolver.</param>
public sealed class MongoDBObserverIdentityClaims(IDatabase database) : IObserverIdentityClaims
{
    /// <summary>
    /// The name of the claims collection in the event store database.
    /// </summary>
    internal const string CollectionName = "observer-identity-claims";

    static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    /// <inheritdoc/>
    public async Task<ObserverIdentityClaim> Claim(EventStoreName store, ObserverIdentityClaim claim)
    {
        var collection = Claims(store);
        var update = Builders<BsonDocument>.Update
            .SetOnInsert("claimant", (int)claim.Claimant)
            .SetOnInsert("projection", claim.Projection.Value)
            .SetOnInsert("generation", (long)claim.Generation.Value)
            .SetOnInsert("revision", claim.Revision.Value);
        try
        {
            var stored = await collection.FindOneAndUpdateAsync(
                _filter.Eq("_id", claim.ObserverId.Value),
                update,
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return Decode(stored);
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await ReadStored(collection, claim.ObserverId);
        }
        catch (MongoCommandException error) when (error.Code == 11000)
        {
            return await ReadStored(collection, claim.ObserverId);
        }
    }

    /// <inheritdoc/>
    public async Task<ObserverIdentityClaim?> Get(EventStoreName store, ObserverId observerId)
    {
        var document = await Claims(store).Find(_filter.Eq("_id", observerId.Value)).FirstOrDefaultAsync();
        return document is null ? null : Decode(document);
    }

    /// <inheritdoc/>
    public async Task<bool> Release(EventStoreName store, ObserverIdentityClaim claim)
    {
        var filter = _filter.Eq("_id", claim.ObserverId.Value)
            & _filter.Eq("claimant", (int)claim.Claimant)
            & _filter.Eq("projection", claim.Projection.Value)
            & _filter.Eq("generation", (long)claim.Generation.Value)
            & _filter.Eq("revision", claim.Revision.Value);
        var result = await Claims(store).DeleteOneAsync(filter);
        return result.DeletedCount == 1;
    }

    /// <inheritdoc/>
    public Task<bool> HasDefinition(EventStoreName store, ObserverId observerId) =>
        new Observation.ObserverDefinitionsStorage(database.GetEventStoreDatabase(store)).Has(observerId);

    static async Task<ObserverIdentityClaim> ReadStored(IMongoCollection<BsonDocument> collection, ObserverId observerId) =>
        Decode(await collection.Find(_filter.Eq("_id", observerId.Value)).FirstAsync());

    static ObserverIdentityClaim Decode(BsonDocument document) => new(
        new ObserverId(document["_id"].AsString),
        (ObserverIdentityClaimant)document["claimant"].AsInt32,
        new ProjectionId(document["projection"].AsString),
        new MaterializationGeneration(checked((uint)document["generation"].AsInt64)),
        new MaterializationRevision(document["revision"].AsString));

    IMongoCollection<BsonDocument> Claims(EventStoreName store) =>
        database.GetEventStoreDatabase(store).GetCollection<BsonDocument>(CollectionName)
            .WithWriteConcern(WriteConcern.WMajority)
            .WithReadConcern(ReadConcern.Majority);
}
