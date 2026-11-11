// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_reusing_an_inherited_schema_with_a_nullable_decimal : Specification
{
    BsonDocument _null;
    BsonDocument _value;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
                "type": "object",
                "allOf": [{"type": "object", "properties": {"inherited": {"type": "number", "format": "decimal"}}}],
                "properties": {"amount": {"oneOf": [{"type": "null"}, {"type": "number", "format": "decimal"}]}}
            }
            """);
        _null = EventContentBson.FromJson("""{"inherited":1.25,"amount":null}""", schema);
        _value = EventContentBson.FromJson("""{"inherited":2.5,"AMOUNT":3.75}""", schema);
    }

    [Fact] void should_preserve_the_null_value() => _null["amount"].BsonType.ShouldEqual(BsonType.Null);
    [Fact] void should_preserve_the_inherited_decimal() => _value["inherited"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_present_nullable_decimal_as_decimal128() => _value["AMOUNT"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_reuse_the_nullable_schema_without_losing_the_value() => _value["AMOUNT"].AsDecimal128.ShouldEqual(new Decimal128(3.75m));
}
