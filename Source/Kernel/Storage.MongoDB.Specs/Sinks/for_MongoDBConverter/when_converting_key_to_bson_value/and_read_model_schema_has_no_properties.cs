// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_key_to_bson_value;

public class and_read_model_schema_has_no_properties : a_mongodb_converter
{
    BsonValue _result;
    Key _key;

    void Establish()
    {
        _model = new ReadModelDefinition(
            "empty-model",
            "empty-model",
            "empty-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
        _converter = new(_expandoObjectConverter, _typeFormats, _model);
        _key = new Key("key-value", ArrayIndexers.NoIndexers);
    }

    void Because() => _result = _converter.ToBsonValue(_key);

    [Fact] void should_return_a_bson_value() => _result.ShouldNotBeNull();
    [Fact] void should_return_string_representation_of_key() => _result.AsString.ShouldEqual("key-value");
}
