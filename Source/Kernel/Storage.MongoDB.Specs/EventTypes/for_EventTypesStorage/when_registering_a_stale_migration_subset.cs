// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_registering_a_stale_migration_subset(MongoDBFixture fixture) : given.a_populated_migration_chain(fixture)
{
    EventTypeDefinition _cold;
    BsonDocument _after;
    bool _changed;
    bool _changedAgain;

    async Task Because()
    {
        _changed = await _storage.Register(_stale);
        _changedAgain = await _storage.Register(_stale);
        _cold = await ReadCold();
        _after = await _storedDocuments.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
    }

    [Fact] Task should_still_upcast_from_one_to_three() => ShouldMigrateAcrossChain(_cold, (1, "one"), (3, "three"));
    [Fact] Task should_still_downcast_from_three_to_one() => ShouldMigrateAcrossChain(_cold, (3, "three"), (1, "one"));
    [Fact] void should_not_report_a_change_when_repeated() => _changedAgain.ShouldBeFalse();
    [Fact] void should_not_report_a_change() => _changed.ShouldBeFalse();
    [Fact] async Task should_keep_the_warm_definition() => (await _storage.GetDefinition(_chain.Id)).ShouldBeSame(_warm);
    [Fact] void should_preserve_the_schema_map() => ShouldRetainSchemas(_cold);
    [Fact] void should_preserve_all_bridges() => ShouldRetainBridges(_cold);
    [Fact] void should_not_rewrite_the_document() => _after.ShouldEqual(_before);
}
