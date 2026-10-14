// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_recording;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class with_a_fresh_collection(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    List<BsonDocument> _indexes;

    async Task Because()
    {
        await _storage.Record(_occurrence);
        using var cursor = await _database.GetCollection<BsonDocument>("occurrences").Indexes.ListAsync();
        _indexes = await cursor.ToListAsync();
    }

    [Fact] void should_index_active_work_by_trigger() => _indexes.Single(index => index["name"] == "trigger-state")["key"].AsBsonDocument.ShouldEqual(new BsonDocument { { "trigger", 1 }, { "state", 1 } });
    [Fact] void should_index_pending_work_by_schedule() => _indexes.Single(index => index["name"] == "schedule-state")["key"].AsBsonDocument.ShouldEqual(new BsonDocument { { "schedule", 1 }, { "state", 1 } });
}
