// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Gates;

/// <summary>Uses B1's gate document and key format without owning a transaction.</summary>
public sealed class MongoDBGenerationGateWrites : IGenerationGateWrites
{
    /// <inheritdoc/>
    public Task<long> Revoke(IMongoDatabase database, IClientSessionHandle session, GenerationGateId gate, CancellationToken ct = default) =>
        BumpEpoch(database, session, (gate.Value, Builders<BsonDocument>.Update.Inc(GenerationGateCollections.Epoch, 1L).SetOnInsert(GenerationGateCollections.Admitted, 0L)), ct);

    /// <inheritdoc/>
    public Task<long> Revoke(IMongoDatabase database, IClientSessionHandle session, string gateId, CancellationToken ct = default) =>
        BumpEpoch(database, session, (gateId, Builders<BsonDocument>.Update.Inc(GenerationGateCollections.Epoch, 1L)), ct);

    /// <inheritdoc/>
    public async Task<long?> CurrentEpoch(IMongoDatabase database, GenerationGateId gate, CancellationToken ct = default)
    {
        var document = await database.GetCollection<BsonDocument>(GenerationGateCollections.Gates).WithReadConcern(ReadConcern.Majority)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", gate.Value)).FirstOrDefaultAsync(ct);
        return document?[GenerationGateCollections.Epoch].AsInt64;
    }

    static async Task<long> BumpEpoch(IMongoDatabase database, IClientSessionHandle session, (string Id, UpdateDefinition<BsonDocument> Update) gate, CancellationToken ct)
    {
        var updated = await database.GetCollection<BsonDocument>(GenerationGateCollections.Gates).FindOneAndUpdateAsync(
            session,
            Builders<BsonDocument>.Filter.Eq("_id", gate.Id),
            gate.Update,
            new() { IsUpsert = true, ReturnDocument = ReturnDocument.After },
            ct);
        return updated[GenerationGateCollections.Epoch].AsInt64;
    }
}
