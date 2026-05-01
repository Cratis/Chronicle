// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class always_creates_subject_index : given.a_sink_with_indexes
{
    void Establish() => _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

    async Task Because() => await _sink.EnsureIndexes();

    [Fact]
    void should_create_subject_index_with_correct_name() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model =>
                model.Options.Name == "subject_index"),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());

    [Fact]
    void should_create_subject_index_as_sparse() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model =>
                model.Options.Sparse == true),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
