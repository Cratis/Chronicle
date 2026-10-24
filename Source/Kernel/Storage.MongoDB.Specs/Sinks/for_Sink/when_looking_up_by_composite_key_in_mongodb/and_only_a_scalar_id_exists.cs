// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key_in_mongodb;

[Collection(MongoDBCollection.Name)]
public class and_only_a_scalar_id_exists(MongoDBFixture fixture) : given.a_mongo_composite_key_container
{
    CompositeKeyLookupResult _result;
    protected override MongoSinkHarness CreateHarness() => _mongo = new() { Fixture = fixture };
    async Task Establish() => await Collection.InsertOneAsync(new BsonDocument { { "_id", "a_b" }, { "count", 42 } });
    async Task Because() => _result = await Lookup(CompositeKey("a", "b"));
    [Fact] void should_not_match_the_scalar_id() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
