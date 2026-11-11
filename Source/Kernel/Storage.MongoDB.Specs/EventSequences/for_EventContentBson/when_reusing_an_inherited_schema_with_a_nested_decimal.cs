// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_reusing_an_inherited_schema_with_a_nested_decimal : Specification
{
    BsonDocument _first;
    BsonDocument _second;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
                "type": "object",
                "allOf": [{"type": "object", "properties": {"inherited": {"type": "number", "format": "decimal"}}}],
                "properties": {
                    "nested": {
                        "type": "object",
                        "allOf": [{"type": "object", "properties": {"amount": {"type": "number", "format": "decimal"}}}],
                        "properties": {"extra": {"type": "number", "format": "decimal"}}
                    }
                }
            }
            """);
        _first = EventContentBson.FromJson("""{"inherited":1.25,"nested":{"amount":2.5,"extra":3.75}}""", schema);
        _second = EventContentBson.FromJson("""{"inherited":4.25,"nested":{"AMOUNT":5.5,"EXTRA":6.75}}""", schema);
    }

    [Fact] void should_preserve_the_inherited_root_decimal() => _second["inherited"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_first_nested_decimal_as_decimal128() => _first["nested"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_reuse_the_nested_schema_case_insensitively() => _second["nested"]["AMOUNT"].AsDecimal128.ShouldEqual(new Decimal128(5.5m));
    [Fact] void should_preserve_properties_declared_alongside_nested_inheritance() => _second["nested"]["EXTRA"].AsDecimal128.ShouldEqual(new Decimal128(6.75m));
}
