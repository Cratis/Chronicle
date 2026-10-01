// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_index_has_a_custom_name_and_explicit_defaults : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        _existingIndex["name"] = "custom_index";
        _existingIndex["unique"] = false;
        _existingIndex["prepareUnique"] = false;
        _existingIndex["sparse"] = false;
        _existingIndex["hidden"] = false;
        _existingIndex["background"] = false;
        _existingIndex["v"] = 2;
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
