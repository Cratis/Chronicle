// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_round_tripping_unsigned_64_bit_values : Specification
{
    BsonDocument _stored;
    JsonNode _read;

    void Because()
    {
        _stored = EventContentBson.FromJson("""
            {"value":18446744073709551615,"values":[18446744073709551615,9223372036854775808],
             "small":9223372036854775807,"nested":{"value":18446744073709551615},"text":"18446744073709551615"}
            """);
        _read = JsonNode.Parse(EventContentBson.ToJson(_stored))!;
    }

    [Fact] void should_store_the_maximum_as_decimal128() => _stored["value"].AsDecimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_keep_existing_int64_representation() => _stored["small"].BsonType.ShouldEqual(BsonType.Int64);
    [Fact] void should_read_the_maximum_exactly() => _read["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_array_elements_exactly() => _read["values"]![0]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_the_first_value_above_int64_exactly() => _read["values"]![1]!.GetValue<ulong>().ShouldEqual((ulong)long.MaxValue + 1);
    [Fact] void should_read_nested_values_exactly() => _read["nested"]!["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_leave_strings_unchanged() => _read["text"]!.GetValue<string>().ShouldEqual("18446744073709551615");
}
