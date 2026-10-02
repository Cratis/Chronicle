// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_existing_indexes_have_incompatible_options : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        var incompatibleOptions = new BsonDocument
        {
            { "unique", true },
            { "prepareUnique", true },
            { "sparse", true },
            { "hidden", true },
            { "expireAfterSeconds", 3600 },
            { "partialFilterExpression", new BsonDocument(_indexedProperty.Path, new BsonDocument("$exists", true)) },
            { "collation", new BsonDocument { { "locale", "en" }, { "strength", 2 } } }
        };
        _indexCursor.Current.Returns(incompatibleOptions.Select(option =>
        {
            var index = _existingIndex.DeepClone().AsBsonDocument;
            index["name"] = option.Name;
            index.Add(option);
            return index;
        }).ToArray());
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_create_the_requested_index() =>
        _indexManager.Received(1).CreateOneAsync(
            Arg.Is<CreateIndexModel<BsonDocument>>(model => model.Options.Name == $"chronicle_idx_{_indexedProperty.Path}"),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
    [Fact] void should_not_drop_any_indexes() => _indexManager.DidNotReceive().DropOneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
