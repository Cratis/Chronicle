// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_existing_indexes_match_the_key_but_have_incompatible_options : given.a_sink_with_an_existing_index
{
    void Establish() => _existingIndex["sparse"] = true;

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_create_the_requested_index() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_not_fetch_the_collection_collation() =>
        _database.DidNotReceive().ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>());
}
