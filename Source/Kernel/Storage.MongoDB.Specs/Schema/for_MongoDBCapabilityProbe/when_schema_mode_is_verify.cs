// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_schema_mode_is_verify : given.a_driver
{
    void Establish()
    {
        _options = new() { Storage = new() { SchemaMode = SchemaMode.Verify } };
        _probe = new(_database, Options.Create(_options), _logger);
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_accept_the_read_only_probe() => _report.Collation.ShouldEqual(MongoDBCapability.Supported);
    [Fact] void should_use_simple_collation() => _ = _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Is<FindOptions<BsonDocument, BsonDocument>>(options => options.Collation.Equals(Collation.Simple) && options.Limit == 1),
        Arg.Any<CancellationToken>());
    [Fact] void should_not_create_a_collection() => _ = _mongo.DidNotReceiveWithAnyArgs().CreateCollectionAsync(default, default, default);
    [Fact] void should_not_create_an_index() => _ = _indexes.DidNotReceiveWithAnyArgs().CreateOneAsync(default(CreateIndexModel<BsonDocument>), default, default);
    [Fact] void should_not_drop_a_collection() => _ = _mongo.DidNotReceiveWithAnyArgs().DropCollectionAsync(default, default);
    [Fact] void should_dispose_the_find_cursor() => _documents.Received(1).Dispose();
    [Fact] void should_dispose_the_change_stream_cursor() => _changes.Received(1).Dispose();
    [Fact] void should_advance_the_change_stream_cursor_once() => _ = _changes.Received(1).MoveNextAsync(Arg.Any<CancellationToken>());
}
