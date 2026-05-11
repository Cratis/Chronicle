// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value_with_property_path;

public class and_property_path_is_not_in_schema : a_mongodb_converter
{
    BsonValue _result;

    void Because() => _result = _converter.ToBsonValue("value", new PropertyPath("not.in.schema"));

    [Fact] void should_return_a_bson_value() => _result.ShouldNotBeNull();
    [Fact] void should_return_string_representation_of_value() => _result.AsString.ShouldEqual("value");
}
