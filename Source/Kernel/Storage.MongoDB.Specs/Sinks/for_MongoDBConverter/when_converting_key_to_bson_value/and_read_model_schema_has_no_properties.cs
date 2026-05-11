// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_key_to_bson_value;

public class and_read_model_schema_has_no_properties : a_mongodb_converter
{
    Exception _exception;
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

    void Because() => _exception = Catch.Exception(() => _converter.ToBsonValue(_key));

    [Fact] void should_throw_invalid_read_model_key_schema() => _exception.ShouldBeOfExactType<InvalidReadModelKeySchema>();
    [Fact] void should_include_read_model_identifier_in_message() => _exception.Message.ShouldContain("empty-model");
}
