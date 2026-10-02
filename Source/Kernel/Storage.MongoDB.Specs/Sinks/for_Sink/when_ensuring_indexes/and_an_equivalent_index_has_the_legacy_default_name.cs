// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_index_has_the_legacy_default_name : given.a_sink_with_an_existing_index
{
    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_not_drop_any_indexes() => _indexManager.DidNotReceive().DropOneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_dispose_the_index_cursor() => _indexCursor.Received().Dispose();
}
