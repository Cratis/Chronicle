// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_a_byte_array_collection_to_bson_value : Specification
{
    List<byte[]> _input;
    BsonValue _result;

    void Establish() => _input = [[], [1], [251, 255]];

    void Because() => _result = _input.ToBsonValue();

    [Fact] void should_store_an_array() => _result.ShouldBeOfExactType<BsonArray>();
    [Fact] void should_preserve_the_element_count() => _result.AsBsonArray.Count.ShouldEqual(3);
    [Fact] void should_preserve_the_empty_binary_value() => _result.AsBsonArray[0].AsBsonBinaryData.Bytes.ShouldBeEmpty();
    [Fact] void should_preserve_the_single_byte() => _result.AsBsonArray[1].AsBsonBinaryData.Bytes.SequenceEqual<byte>([1]).ShouldBeTrue();
    [Fact] void should_preserve_the_multiple_bytes() => _result.AsBsonArray[2].AsBsonBinaryData.Bytes.SequenceEqual<byte>([251, 255]).ShouldBeTrue();
}
