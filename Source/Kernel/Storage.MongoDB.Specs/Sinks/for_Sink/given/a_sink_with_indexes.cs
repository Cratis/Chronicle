// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class a_sink_with_indexes : Specification
{
    protected Sink _sink;
    protected IMongoDBConverter _converter;
    protected ISinkCollections _collections;
    protected IChangesetConverter _changesetConverter;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected IMongoCollection<BsonDocument> _collection;
    protected IMongoIndexManager<BsonDocument> _indexManager;
    protected IAsyncCursor<BsonDocument> _indexCursor;
    protected ReadModelDefinition _readModel;
    protected PropertyPath _indexedProperty = "SomeProperty";

    void Establish()
    {
        _converter = Substitute.For<IMongoDBConverter>();
        _collections = Substitute.For<ISinkCollections>();
        _changesetConverter = Substitute.For<IChangesetConverter>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _indexManager = Substitute.For<IMongoIndexManager<BsonDocument>>();
        _indexCursor = Substitute.For<IAsyncCursor<BsonDocument>>();

        _collections.GetCollection().Returns(_collection);
        _collection.Indexes.Returns(_indexManager);
        _indexManager.ListAsync(Arg.Any<CancellationToken>()).Returns(_indexCursor);

        _readModel = new ReadModelDefinition(
            "SomethingId",
            "Something",
            "Something",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            [new IndexDefinition(_indexedProperty)]);

        _sink = new Sink(
            _readModel,
            _converter,
            _collections,
            _changesetConverter,
            _expandoObjectConverter);
    }
}
