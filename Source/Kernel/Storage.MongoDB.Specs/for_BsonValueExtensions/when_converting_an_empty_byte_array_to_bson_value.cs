// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_an_empty_byte_array_to_bson_value : Specification
{
    BsonValue _result;

    void Because() => _result = Array.Empty<byte>().ToBsonValue();

    [Fact] void should_store_binary_data_instead_of_an_array() => _result.ShouldBeOfExactType<BsonBinaryData>();
    [Fact] void should_preserve_the_empty_value() => _result.AsBsonBinaryData.Bytes.ShouldBeEmpty();
}
