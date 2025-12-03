// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value;

public class and_value_is_expando_object : given.a_mongodb_converter
{
    ExpandoObject _value;
    BsonValue _result;

    void Establish()
    {
        _value = new ExpandoObject();
        ((dynamic)_value).SomeProperty = "Some value";
    }

    void Because() => _result = _converter.ToBsonValue(_value);

    [Fact] void should_return_a_bson_document() => _result.ShouldNotBeNull();
    [Fact] void should_have_the_correct_value() => _result.AsBsonDocument["SomeProperty"].AsString.ShouldEqual("Some value");
}
