// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_restoring_content_with_ordinary_and_mixed_arrays : Specification
{
    BsonDocument _stored;
    JsonNode _read;

    void Establish() => _stored = BsonDocument.Parse("""
        {"strings":["a","b"],"integers":[1,2],"objects":[{"a":[1]}],
         "mixed":[1,{"$numberDecimal":"18446744073709551615"},"x",{"a":[1]}]}
        """);

    void Because() => _read = JsonNode.Parse(EventContentBson.ToJson(_stored))!;

    [Fact] void should_preserve_strings() => _read["strings"]![0]!.GetValue<string>().ShouldEqual("a");
    [Fact] void should_preserve_small_integers() => _read["integers"]![1]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_preserve_nested_objects() => _read["objects"]![0]!["a"]![0]!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_restore_the_large_unsigned_element() => _read["mixed"]![1]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_the_mixed_array() => JsonNode.DeepEquals(_read["mixed"], JsonNode.Parse("""[1,18446744073709551615,"x",{"a":[1]}]""")).ShouldBeTrue();
}
