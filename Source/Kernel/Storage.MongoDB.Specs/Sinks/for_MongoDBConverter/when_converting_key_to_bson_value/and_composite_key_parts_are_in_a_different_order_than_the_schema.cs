// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_key_to_bson_value;

public class and_composite_key_parts_are_in_a_different_order_than_the_schema : a_mongodb_converter
{
    record CompositeKey(string First, string Second);

    record ModelWithCompositeKey(CompositeKey Id, string Name);

    BsonValue _result;

    void Establish()
    {
        var schema = JsonSchema.FromType<ModelWithCompositeKey>();

        _model = new ReadModelDefinition(
            "composite-model",
            "composite-model",
            "composite-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
            []);

        // The projection mapping order is the reverse of the schema order.
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new BsonDocument { { "second", "b" }, { "first", "a" } });

        _converter = new(_expandoObjectConverter, _typeFormats, _model, _logger);
    }

    void Because() => _result = _converter.ToBsonValue(new Key(new ExpandoObject(), ArrayIndexers.NoIndexers));

    [Fact] void should_write_parts_in_schema_order() => _result.AsBsonDocument.Names.ShouldEqual(["first", "second"]);
}
