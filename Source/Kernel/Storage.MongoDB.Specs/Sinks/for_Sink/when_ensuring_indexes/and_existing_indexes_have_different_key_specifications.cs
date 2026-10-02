// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_existing_indexes_have_different_key_specifications : given.a_sink_with_an_existing_index
{
    void Establish() => _indexCursor.Current.Returns(
    [
        new BsonDocument { { "name", "_id_" }, { "key", new BsonDocument("_id", 1) } },
        new BsonDocument { { "name", "descending" }, { "key", new BsonDocument(_indexedProperty.Path, -1) } },
        new BsonDocument { { "name", "compound" }, { "key", new BsonDocument { { _indexedProperty.Path, 1 }, { "OtherProperty", 1 } } } },
        new BsonDocument { { "name", $"chronicle_idx_{_indexedProperty.Path}" }, { "key", new BsonDocument("OtherProperty", 1) } },
        new BsonDocument { { "name", "hashed" }, { "key", new BsonDocument(_indexedProperty.Path, "hashed") } }
    ]);

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_create_the_requested_index() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model => model.Options.Name == $"chronicle_idx_{_indexedProperty.Path}"),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_not_fetch_the_collection_collation() =>
        _database.DidNotReceive().ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>());
}
