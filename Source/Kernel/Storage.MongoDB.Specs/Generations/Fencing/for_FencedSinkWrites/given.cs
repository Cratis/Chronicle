// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

public class given(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    protected FencedSinkWrites _writes;
    protected IMongoCollection<BsonDocument> _models;

    async Task Establish()
    {
        _writes = new(new FixedFenceTokenSource(_token), _executor);
        await _database.CreateCollectionAsync("models");
        _models = _database.GetCollection<BsonDocument>("models");
    }

    protected static WriteModel<BsonDocument> Insert(int id) => new InsertOneModel<BsonDocument>(new BsonDocument("_id", id));

    protected Task<long> Count() => _models.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
}
