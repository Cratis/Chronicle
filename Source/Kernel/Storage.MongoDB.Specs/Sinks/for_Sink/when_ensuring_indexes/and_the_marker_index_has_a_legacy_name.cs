// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

[Collection(MongoDBCollection.Name)]
public class and_the_marker_index_has_a_legacy_name(MongoDBFixture fixture) : Contract.given.a_populated_sink<MongoSinkHarness>
{
    MongoSinkHarness _harness;
    string[] _indexNames;

    protected override MongoSinkHarness CreateHarness() => _harness = new() { Fixture = fixture };

    Task Establish() => _harness.Database.GetCollection<BsonDocument>("paged_read_models").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
        new BsonDocument(WellKnownProperties.ReadModelInstanceInitialized, 1),
        new CreateIndexOptions { Name = "legacy_initialized_marker" }));

    async Task Because()
    {
        await _sink.EnsureIndexes();
        await _sink.EnsureIndexes();
        using var cursor = await _harness.Database.GetCollection<BsonDocument>("paged_read_models").Indexes.ListAsync();
        _indexNames = (await cursor.ToListAsync()).Select(index => index["name"].AsString).ToArray();
    }

    [Fact] void should_preserve_the_legacy_marker_index() => _indexNames.ShouldContain("legacy_initialized_marker");
    [Fact] void should_not_create_a_duplicate_marker_index() => _indexNames.Length.ShouldEqual(2);
    [Fact] void should_not_create_a_named_marker_index() => _indexNames.ShouldNotContain("chronicle_idx___initialized");
}
