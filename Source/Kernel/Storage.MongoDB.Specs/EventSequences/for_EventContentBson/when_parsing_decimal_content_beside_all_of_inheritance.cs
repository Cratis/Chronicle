// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_decimal_content_beside_all_of_inheritance : Specification
{
    JsonSchema _schema;
    BsonDocument _stored;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "type": "object",
          "allOf": [{"$ref": "#/$defs/Base"}],
          "properties": {
            "amount": {"type": "number", "format": "decimal"},
            "nested": {
              "type": "object",
              "properties": {"amount": {"type": "number", "format": "decimal"}}
            },
            "values": {"type": "array", "items": {"type": "number", "format": "decimal"}},
            "nullable": {"oneOf": [{"type": "null"}, {"type": "number", "format": "decimal"}]},
            "absent": {"type": ["number", "null"], "format": "decimal?"},
            "optional": {"type": ["number", "null"], "format": "decimal?"},
            "ratio": {"type": "number", "format": "double"},
            "referenced": {"$ref": "#/$defs/Derived"}
          },
          "$defs": {
            "Base": {
              "type": "object",
              "properties": {"inherited": {"type": "number", "format": "decimal"}}
            },
            "Derived": {
              "type": "object",
              "allOf": [{"$ref": "#/$defs/Base"}],
              "properties": {"amount": {"type": "number", "format": "decimal"}}
            }
          }
        }
        """);

    void Because() => _stored = EventContentBson.FromJson(
        """
        {
          "inherited": 3.75,
          "amount": 3.75,
          "nested": {"amount": 3.75},
          "values": [3.75, 7.5],
          "nullable": 3.75,
          "absent": null,
          "optional": 3.75,
          "ratio": 3.75,
          "referenced": {"inherited": 3.75, "amount": 3.75}
        }
        """,
        _schema);

    [Fact] void should_store_the_direct_decimal_as_decimal128() => _stored["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_nested_decimal_as_decimal128() => _stored["nested"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_first_array_decimal_as_decimal128() => _stored["values"][0].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_second_array_decimal_as_decimal128() => _stored["values"][1].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_nullable_decimal_as_decimal128() => _stored["nullable"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_optional_decimal_as_decimal128() => _stored["optional"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_null() => _stored["absent"].BsonType.ShouldEqual(BsonType.Null);
    [Fact] void should_preserve_the_inherited_decimal_format() => _stored["inherited"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_the_double_format() => _stored["ratio"].BsonType.ShouldEqual(BsonType.Double);
    [Fact] void should_preserve_the_decimal_value() => _stored["amount"].AsDecimal128.ShouldEqual(new Decimal128(3.75m));
    [Fact] void should_store_the_referenced_decimal_as_decimal128() => _stored["referenced"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_the_referenced_inherited_decimal_format() => _stored["referenced"]["inherited"].BsonType.ShouldEqual(BsonType.Decimal128);
}
