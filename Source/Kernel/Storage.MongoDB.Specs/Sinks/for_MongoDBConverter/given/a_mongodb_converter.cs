// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;

public class a_mongodb_converter : Specification
{
    protected MongoDBConverter _converter;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ITypeFormats _typeFormats;
    protected ReadModelDefinition _model;

    void Establish()
    {
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _typeFormats = Substitute.For<ITypeFormats>();
        _model = new ReadModelDefinition(
            typeof(ReadModel).FullName,
            nameof(ReadModel),
            nameof(ReadModel),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromType<ReadModel>() },
            },
            []);
        _converter = new(_expandoObjectConverter, _typeFormats, _model);
    }
}
