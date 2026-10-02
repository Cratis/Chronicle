// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_listing_indexes_fails : given.a_sink_with_indexes
{
    Exception _failure;
    Exception _error;

    void Establish()
    {
        _failure = new TimeoutException("Listing indexes failed");
        _indexManager.ListAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncCursor<BsonDocument>>(_failure));
    }

    async Task Because() => _error = await Catch.Exception(_sink.EnsureIndexes);

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
