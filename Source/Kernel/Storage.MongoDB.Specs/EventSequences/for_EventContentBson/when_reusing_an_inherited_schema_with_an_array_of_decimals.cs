// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_reusing_an_inherited_schema_with_an_array_of_decimals : Specification
{
    BsonDocument _first;
    BsonDocument _second;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
                "$defs": {"base": {"type": "object", "properties": {"inherited": {"type": "number", "format": "decimal"}}}},
                "type": "object",
                "allOf": [{"$ref": "#/$defs/base"}],
                "properties": {"values": {"type": "array", "items": {"type": "number", "format": "decimal"}}}
            }
            """);
        _first = EventContentBson.FromJson("""{"inherited":1.25,"values":[2.5,3.75]}""", schema);
        _second = EventContentBson.FromJson("""{"inherited":4.25,"VALUES":[5.5,6.75]}""", schema);
    }

    [Fact] void should_preserve_the_referenced_inherited_decimal() => _second["inherited"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_first_array_decimal_as_decimal128() => _first["values"][0].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_reuse_the_array_schema_case_insensitively() => _second["VALUES"][0].AsDecimal128.ShouldEqual(new Decimal128(5.5m));
    [Fact] void should_preserve_each_array_decimal_value() => _second["VALUES"][1].AsDecimal128.ShouldEqual(new Decimal128(6.75m));
}
