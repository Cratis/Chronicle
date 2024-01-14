// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value;

public class and_value_is_expando_object : given.a_mongodb_converter
{
    ExpandoObject value;
    BsonValue result;

    void Establish()
    {
        value = new ExpandoObject();
        ((dynamic)value).SomeProperty = "Some value";
    }

    void Because() => result = converter.ToBsonValue(value);

    [Fact] void should_return_a_bson_document() => result.ShouldNotBeNull();
    [Fact] void should_have_the_correct_value() => result.AsBsonDocument["SomeProperty"].AsString.ShouldEqual("Some value");
}
