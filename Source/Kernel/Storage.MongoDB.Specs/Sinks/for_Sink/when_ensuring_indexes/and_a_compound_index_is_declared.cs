// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_a_compound_index_is_declared : given.a_sink_with_indexes
{
    readonly BsonDocument _expected = new() { ["customerId"] = 1, ["details.created"] = -1 };

    void Establish()
    {
        _readModel = _readModel with { Indexes = [new IndexDefinition("customerId") { Properties = [new("customerId"), new("details.created", true)] }] };
        _sink = new(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
    }

    Task Because() => _sink.EnsureIndexes();

    [Fact]
    void should_send_one_ordered_key_document() => _indexManager.Received(1).CreateOneAsync(
        Arg.Is<CreateIndexModel<BsonDocument>>(model => Keys(model).Equals(_expected) && model.Options.Name == ReadModelIndexKeys.Name(_readModel.Indexes.Single())),
        Arg.Any<CreateOneIndexOptions>(),
        Arg.Any<CancellationToken>());

    static BsonDocument Keys(CreateIndexModel<BsonDocument> model) => model.Keys.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));
}
