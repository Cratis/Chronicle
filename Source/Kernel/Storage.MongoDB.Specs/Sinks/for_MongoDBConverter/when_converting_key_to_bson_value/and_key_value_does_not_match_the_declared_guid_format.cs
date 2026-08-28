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

/// <summary>
/// Reproduces #3844 - a read model whose key property schema reports the "guid" format (a plain
/// Guid-typed Id, the same shape a ConceptAs&lt;string&gt; key ends up with) must not crash the partition
/// when the actual key value, such as an organization name, is not a Guid.
/// </summary>
public class and_key_value_does_not_match_the_declared_guid_format : a_mongodb_converter
{
    BsonValue _result;
    Key _key;

    void Establish()
    {
        _model = new ReadModelDefinition(
            "string-keyed-model",
            "string-keyed-model",
            "string-keyed-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromType<GuidKeyedReadModel>() }
            },
            []);
        _typeFormats = new TypeFormats();
        _converter = new(_expandoObjectConverter, _typeFormats, _model, _logger);
        _key = new Key("Powerworks", ArrayIndexers.NoIndexers);
    }

    void Because() => _result = _converter.ToBsonValue(_key);

    [Fact] void should_return_a_bson_value() => _result.ShouldNotBeNull();
    [Fact] void should_fall_back_to_the_untyped_string_representation() => _result.AsString.ShouldEqual("Powerworks");
}
