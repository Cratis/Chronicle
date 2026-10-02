// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_index_targets_a_nested_property : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        _indexedProperty = "Parent.SomeProperty";
        _readModel = _readModel with { Indexes = [new IndexDefinition(_indexedProperty)] };
        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
        _existingIndex["name"] = $"{_indexedProperty.Path}_1";
        _existingIndex["key"] = new BsonDocument(_indexedProperty.Path, 1);
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_not_create_the_index() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
