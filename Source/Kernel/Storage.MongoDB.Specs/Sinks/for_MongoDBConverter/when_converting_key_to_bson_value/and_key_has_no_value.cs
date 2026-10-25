// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_key_to_bson_value;

public class and_key_has_no_value : a_mongodb_converter
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => _converter.ToBsonValue(new Key(null!, ArrayIndexers.NoIndexers)));

    [Fact] void should_throw_read_model_key_has_no_value() => _result.ShouldBeOfExactType<ReadModelKeyHasNoValue>();
    [Fact] void should_name_the_read_model() => _result.Message.Contains(_model.Identifier.ToString()).ShouldBeTrue();
}
