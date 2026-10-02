// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_indexes_already_exist : given.a_sink_with_an_existing_index
{
    void Establish() => _existingIndex["name"] = $"chronicle_idx_{_indexedProperty.Path}";

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_not_fetch_the_collection_collation() =>
        _database.DidNotReceive().ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>());
}
