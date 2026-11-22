// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

internal sealed partial class V2Schema
{
    async Task Bootstrap(CancellationToken cancellationToken)
    {
        var transactions = new V2Transactions(database, ReadConcern.Local);
        try
        {
            await transactions.Run(async session =>
            {
                await Create(session, Authorities, _metadataIndexes, cancellationToken);
                await Create(session, Bindings, _bindingIndexes, cancellationToken);
                await database.GetCollection<BsonDocument>(Authorities).InsertOneAsync(session, V2Documents.Metadata(Authorities), cancellationToken: cancellationToken);
                await database.GetCollection<BsonDocument>(Bindings).InsertOneAsync(session, V2Documents.Metadata(Bindings), cancellationToken: cancellationToken);
                return (true, true);
            }, cancellationToken);
        }
        catch (MongoException error) when (IsSchemaRace(error))
        {
            // A racing winner is accepted only after complete, read-only ownership and shape verification.
            if (!await EnsureMetadata(false, cancellationToken)) throw;
        }
    }

    async Task Provision(V2Binding binding, CancellationToken cancellationToken)
    {
        var transactions = new V2Transactions(database, ReadConcern.Local);
        try
        {
            await transactions.Run(async session =>
            {
                await Create(session, binding.Physical, _claimIndexes, cancellationToken);
                await database.GetCollection<BsonDocument>(Bindings).InsertOneAsync(session, binding.Document, cancellationToken: cancellationToken);
                return (true, true);
            }, cancellationToken);
        }
        catch (MongoException error) when (IsSchemaRace(error))
        {
            if (await Resolve(binding, false, cancellationToken) is null) throw;
        }
    }

    async Task Create(IClientSessionHandle session, string name, BsonDocument[] indexes, CancellationToken cancellationToken)
    {
        // No catalog reads or existing-collection index repairs occur inside schema transactions.
        await database.CreateCollectionAsync(session, name, new CreateCollectionOptions { Collation = Simple }, cancellationToken);
        var models = indexes.Where(index => index["name"] != "_id_").Select(index => new CreateIndexModel<BsonDocument>(
            new BsonDocumentIndexKeysDefinition<BsonDocument>(index["key"].AsBsonDocument),
            new CreateIndexOptions { Name = index["name"].AsString, Unique = index["unique"].AsBoolean, Collation = Simple }));
        if (indexes.Length > 1) await database.GetCollection<BsonDocument>(name).Indexes.CreateManyAsync(session, models, cancellationToken);
    }

    static bool IsSchemaRace(MongoException error) => V2Transactions.IsDuplicate(error) || error is MongoCommandException { Code: 48 };
}
