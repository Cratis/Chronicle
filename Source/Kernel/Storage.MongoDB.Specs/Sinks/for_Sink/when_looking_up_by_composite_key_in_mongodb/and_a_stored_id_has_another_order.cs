// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key_in_mongodb;

[Collection(MongoDBCollection.Name)]
public class and_a_stored_id_has_another_order(MongoDBFixture fixture) : given.a_mongo_composite_key_container
{
    CompositeKeyLookupResult _result;
    protected override MongoSinkHarness CreateHarness() => _mongo = new() { Fixture = fixture };
    async Task Establish() => await Collection.InsertOneAsync(new BsonDocument { { "_id", new BsonDocument { { "second", "b" }, { "first", "a" } } }, { "count", 42 } });
    async Task Because() => _result = await Lookup(CompositeKey("a", "b"));
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_return_its_state() => Count(_result).ShouldEqual(42);
}
