// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_to_bson_value_with_property_path;

public class and_property_path_is_not_in_schema : a_mongodb_converter
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _converter.ToBsonValue("value", new PropertyPath("not.in.schema")));

    [Fact] void should_throw_invalid_read_model_property_path() => _exception.ShouldBeOfExactType<InvalidReadModelPropertyPath>();
    [Fact] void should_include_property_path_in_message() => _exception.Message.ShouldContain("not.in.schema");
}

