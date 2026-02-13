// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.given;

public class a_changeset_converter : Specification
{
    protected ChangesetConverter _converter;
    protected IMongoDBConverter _mongoDBConverter;
    protected ISinkCollections _sinkCollections;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected IMongoCollection<BsonDocument> _collection;
    protected ReadModelDefinition _readModel;

    void Establish()
    {
        var generator = new NJsonSchemaGenerator(new SystemTextJsonSchemaGeneratorSettings());

        _mongoDBConverter = Substitute.For<IMongoDBConverter>();
        _sinkCollections = Substitute.For<ISinkCollections>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();

        _sinkCollections.GetCollection().Returns(_collection);

        _readModel = new ReadModelDefinition(
            typeof(TestReadModel).FullName,
            nameof(TestReadModel),
            nameof(TestReadModel),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, generator.Generate(typeof(TestReadModel)) },
            },
            []);

        _converter = new ChangesetConverter(
            _readModel,
            _mongoDBConverter,
            _sinkCollections,
            _expandoObjectConverter);
    }
}
