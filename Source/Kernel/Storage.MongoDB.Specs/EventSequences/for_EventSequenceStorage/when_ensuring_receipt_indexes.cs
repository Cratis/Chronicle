// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_ensuring_receipt_indexes(ReplicaSetMongoDBFixture fixture) : given.a_receipt_storage(fixture)
{
    BsonDocument _index;

    async Task Because()
    {
        await _storage.EnsureIndexes();
        using var cursor = await _collection.Database.GetCollection<BsonDocument>("effect-receipts").Indexes.ListAsync();
        _index = (await cursor.ToListAsync()).Single(index => index["name"] == "occurrenceId");
    }

    [Fact] void should_index_the_occurrence_identity() => _index["key"].AsBsonDocument.ShouldEqual(new BsonDocument("occurrenceId", 1));
}
