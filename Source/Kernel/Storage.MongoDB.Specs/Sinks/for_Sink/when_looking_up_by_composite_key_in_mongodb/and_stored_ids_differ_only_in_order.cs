// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key_in_mongodb;

[Collection(MongoDBCollection.Name)]
public class and_stored_ids_differ_only_in_order(MongoDBFixture fixture) : given.a_mongo_composite_key_container
{
    CompositeKeyLookupResult _result;
    protected override MongoSinkHarness CreateHarness() => _mongo = new() { Fixture = fixture };
    async Task Establish() => await Collection.InsertManyAsync([
        new BsonDocument { { "_id", new BsonDocument { { "first", "a" }, { "second", "b" } } }, { "count", 1 } },
        new BsonDocument { { "_id", new BsonDocument { { "second", "b" }, { "first", "a" } } }, { "count", 2 } }
    ]);
    async Task Because() => _result = await Lookup(CompositeKey("a", "b"));
    [Fact] void should_report_ambiguity() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] void should_not_choose_an_instance() => _result.Instance.ShouldBeNull();
}
