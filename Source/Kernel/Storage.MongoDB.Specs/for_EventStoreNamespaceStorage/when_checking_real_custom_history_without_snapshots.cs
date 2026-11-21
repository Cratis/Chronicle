// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

[Trait("Category", "Container")]
[Collection(MongoDBCollection.Name)]
public class when_checking_real_custom_history_without_snapshots(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    bool _hasData;

    Task Establish() => _rawDatabase.GetCollection<BsonDocument>("custom-events").InsertOneAsync(new BsonDocument("_id", 0));

    async Task Because() => _hasData = await _database.HasEvents();

    [Fact] void should_find_the_event_without_a_sequence_snapshot() => _hasData.ShouldBeTrue();
}
