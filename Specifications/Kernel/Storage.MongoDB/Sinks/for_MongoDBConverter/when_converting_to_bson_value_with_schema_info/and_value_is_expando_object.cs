// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Schemas;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value_with_schema_info;

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
        schema_property = model.Schema.GetSchemaPropertyForPropertyPath(nameof(given.ReadModel.SomeProperty));

        bson_document = new()
        {
            { "someProperty", "Some value" }
        };
        expando_object_converter.Setup(_ => _.ToBsonDocument(value, schema_property)).Returns(bson_document);
    }

    void Because() => result = converter.ToBsonValue(value, schema_property);

    [Fact] void should_convert_using_expando_object_converter() => expando_object_converter.Verify(_ => _.ToBsonDocument(value, schema_property), Once);
    [Fact] void should_return_the_converted_object() => result.ShouldEqual(bson_document);
}
