// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_existing_index_has_explicit_simple_collation_but_the_collection_does_not : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        _existingIndex["collation"] = new BsonDocument("locale", "simple");
        _collectionOptions["collation"] = new BsonDocument { { "locale", "en" }, { "strength", 2 } };
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact]
    void should_create_the_requested_index() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model => model.Options.Name == $"chronicle_idx_{_indexedProperty.Path}"),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
