// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchemaGenerator = NJsonSchema.Generation.JsonSchemaGenerator;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;

public class a_mongodb_converter : Specification
{
    protected MongoDBConverter _converter;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ITypeFormats _typeFormats;
    protected ReadModelDefinition _model;

    void Establish()
    {
        var generator = new NJsonSchemaGenerator(new SystemTextJsonSchemaGeneratorSettings());

        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _typeFormats = Substitute.For<ITypeFormats>();
        _model = new ReadModelDefinition(
            typeof(ReadModel).FullName!,
            nameof(ReadModel),
            ReadModelOwner.Client,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, generator.Generate(typeof(ReadModel)) },
            },
            []);
        _converter = new(_expandoObjectConverter, _typeFormats, _model);
    }
}
