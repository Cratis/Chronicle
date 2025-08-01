// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value_with_schema_info;

public class and_value_is_expando_object : given.a_mongodb_converter
{
    ExpandoObject _value;
    BsonValue _result;
    JsonSchemaProperty _schemaProperty;
    BsonDocument _bsonDocument;

    void Establish()
    {
        _value = new ExpandoObject();
        ((dynamic)_value).SomeProperty = "Some value";
        _schemaProperty = _model.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(nameof(given.ReadModel.SomeProperty));

        _bsonDocument = new()
        {
            { "someProperty", "Some value" }
        };
        _expandoObjectConverter.ToBsonDocument(_value, _schemaProperty).Returns(_bsonDocument);
    }

    void Because() => _result = _converter.ToBsonValue(_value, _schemaProperty);

    [Fact] void should_convert_using_expando_object_converter() => _expandoObjectConverter.Received(1).ToBsonDocument(_value, _schemaProperty);
    [Fact] void should_return_the_converted_object() => _result.ShouldEqual(_bsonDocument);
}
