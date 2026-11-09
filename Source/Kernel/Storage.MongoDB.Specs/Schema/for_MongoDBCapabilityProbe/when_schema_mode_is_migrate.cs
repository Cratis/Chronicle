// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_schema_mode_is_migrate : given.a_driver
{
    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_support_collation() => _report.Collation.ShouldEqual(MongoDBCapability.Supported);
    [Fact] void should_create_a_temporary_collection() => _ = _mongo.Received(1).CreateCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_create_a_simple_collation_index() => _ = _indexes.Received(1).CreateOneAsync(
        Arg.Is<CreateIndexModel<BsonDocument>>(model => model.Options.Collation.Equals(Collation.Simple)),
        Arg.Any<CreateOneIndexOptions>(),
        Arg.Any<CancellationToken>());
    [Fact] void should_drop_the_temporary_collection_and_its_index() => _ = _mongo.Received(1).DropCollectionAsync(MongoDBCapabilityProbe.ProbeCollectionName, Arg.Any<CancellationToken>());
    [Fact] void should_not_use_the_read_only_probe() => _ = _collection.DidNotReceiveWithAnyArgs().FindAsync(default(FilterDefinition<BsonDocument>)!, default(FindOptions<BsonDocument, BsonDocument>)!, default);
    [Fact] void should_dispose_the_change_stream_cursor() => _changes.Received(1).Dispose();
    [Fact] void should_dispose_the_collection_listing_cursor() => _collections.Received().Dispose();
}
