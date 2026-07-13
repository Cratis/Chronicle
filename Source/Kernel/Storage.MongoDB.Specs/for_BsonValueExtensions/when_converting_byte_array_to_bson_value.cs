// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_byte_array_to_bson_value : Specification
{
    byte[] _bytes;
    BsonValue _result;

    void Establish() => _bytes = [1, 2, 3, 255, 0];

    void Because() => _result = _bytes.ToBsonValue();

    [Fact] void should_return_bson_binary_data() => _result.ShouldBeOfExactType<BsonBinaryData>();
    [Fact] void should_store_the_bytes() => _result.AsBsonBinaryData.Bytes.ShouldContainOnly(_bytes);
    [Fact] void should_round_trip_back_to_the_same_bytes() => ((byte[])_result.ToTargetType(typeof(byte[]))!).ShouldContainOnly(_bytes);
}
