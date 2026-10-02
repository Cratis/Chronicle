// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_index_has_explicit_simple_collation : given.a_sink_with_an_existing_index
{
    void Establish() => _existingIndex["collation"] = new BsonDocument("locale", "simple");

    async Task Because() => await _sink.EnsureIndexes();

    [Fact]
    void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
