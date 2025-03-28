// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value_with_schema_info;

public class and_value_is_expando_object : given.a_mongodb_converter
{
    ExpandoObject value;
    BsonValue result;
    JsonSchemaProperty schema_property;
    BsonDocument bson_document;

    void Establish()
    {
        value = new ExpandoObject();
        ((dynamic)value).SomeProperty = "Some value";
        schema_property = _model.Schema.GetSchemaPropertyForPropertyPath(nameof(given.ReadModel.SomeProperty));

        bson_document = new()
        {
            { "someProperty", "Some value" }
        };
        _expandoObjectConverter.ToBsonDocument(value, schema_property).Returns(bson_document);
    }

    void Because() => result = _converter.ToBsonValue(value, schema_property);

    [Fact] void should_convert_using_expando_object_converter() => _expandoObjectConverter.Received(1).ToBsonDocument(value, schema_property);
    [Fact] void should_return_the_converted_object() => result.ShouldEqual(bson_document);
}
