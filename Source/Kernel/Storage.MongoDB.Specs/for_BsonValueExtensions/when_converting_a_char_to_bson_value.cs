// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_a_char_to_bson_value : Specification
{
    BsonValue _result;

    void Because() => _result = '\u03a9'.ToBsonValue();

    [Fact] void should_store_a_string() => _result.ShouldBeOfExactType<BsonString>();
    [Fact] void should_preserve_the_character() => _result.AsString.ShouldEqual("\u03a9");
}
