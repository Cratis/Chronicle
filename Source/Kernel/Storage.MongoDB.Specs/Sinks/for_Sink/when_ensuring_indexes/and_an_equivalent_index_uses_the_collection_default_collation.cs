// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_index_uses_the_collection_default_collation : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        var collation = new BsonDocument { { "locale", "en" }, { "strength", 2 } };
        _collectionOptions["collation"] = collation;
        _existingIndex["collation"] = collation;
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_read_the_options_for_the_target_collection() =>
        _database.Received(1).ListCollectionsAsync(
            Arg.Is<ListCollectionsOptions>(options => options.Filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).Equals(new BsonDocument("name", "Something"))),
            Arg.Any<CancellationToken>());
    [Fact] void should_dispose_the_collection_cursor() => _collectionCursor.Received().Dispose();
}
