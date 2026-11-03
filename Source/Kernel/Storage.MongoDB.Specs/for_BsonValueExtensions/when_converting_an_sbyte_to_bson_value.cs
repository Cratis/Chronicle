// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_an_sbyte_to_bson_value : Specification
{
    BsonValue _result;

    void Because() => _result = sbyte.MinValue.ToBsonValue();

    [Fact] void should_store_an_integer() => _result.ShouldBeOfExactType<BsonInt32>();
    [Fact] void should_preserve_the_sign_and_value() => _result.AsInt32.ShouldEqual(-128);
}
