// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

[Collection(MongoDBCollection.Name)]
public class and_no_indexes_are_declared(MongoDBFixture fixture) : Contract.given.a_populated_sink<MongoSinkHarness>
{
    MongoSinkHarness _harness;
    BsonDocument[] _indexes;
    BsonDocument _markerIndex;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { Fixture = fixture };

    async Task Because()
    {
        await _sink.EnsureIndexes();
        await _sink.EnsureIndexes();
        using var cursor = await _harness.Database.GetCollection<BsonDocument>("paged_read_models").Indexes.ListAsync();
        _indexes = (await cursor.ToListAsync()).ToArray();
        _markerIndex = _indexes.Single(index => index["name"] == "chronicle_idx___initialized");
    }

    [Fact] void should_have_only_the_id_and_marker_indexes() => _indexes.Length.ShouldEqual(2);
    [Fact] void should_index_the_marker_in_ascending_order() => _markerIndex["key"].ShouldEqual(new BsonDocument(WellKnownProperties.ReadModelInstanceInitialized, 1));
    [Fact] void should_not_be_unique() => _markerIndex.GetValue("unique", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_not_be_sparse() => _markerIndex.GetValue("sparse", false).AsBoolean.ShouldBeFalse();
    [Fact] void should_not_have_a_partial_filter() => _markerIndex.Contains("partialFilterExpression").ShouldBeFalse();
    [Fact] void should_not_have_a_ttl() => _markerIndex.Contains("expireAfterSeconds").ShouldBeFalse();
    [Fact] void should_not_be_hidden() => _markerIndex.GetValue("hidden", false).AsBoolean.ShouldBeFalse();
}
