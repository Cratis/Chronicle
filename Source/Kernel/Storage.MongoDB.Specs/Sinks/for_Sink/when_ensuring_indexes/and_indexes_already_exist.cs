// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_indexes_already_exist : given.a_sink_with_indexes
{
    void Establish()
    {
        var existingIndexDocument = new BsonDocument("name", $"chronicle_idx_{_indexedProperty.Path}");
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true), Task.FromResult(false));
        _indexCursor.Current.Returns([existingIndexDocument]);
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
