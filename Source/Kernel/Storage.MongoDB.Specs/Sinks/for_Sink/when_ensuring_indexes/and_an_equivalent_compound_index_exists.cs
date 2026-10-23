// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_an_equivalent_compound_index_exists : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        _readModel = _readModel with { Indexes = [new IndexDefinition("customerId") { Properties = [new("customerId"), new("details.created", true)] }] };
        _sink = new(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
        _existingIndex["name"] = "custom_compound";
        _existingIndex["key"] = new BsonDocument { ["customerId"] = 1, ["details.created"] = -1 };
    }

    Task Because() => _sink.EnsureIndexes();

    [Fact]
    void should_preserve_the_equivalent_index() => _indexManager.DidNotReceive().CreateOneAsync(
        Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>());
}
