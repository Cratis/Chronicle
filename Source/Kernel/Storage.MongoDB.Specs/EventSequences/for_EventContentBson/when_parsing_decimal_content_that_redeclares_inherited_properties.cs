// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_decimal_content_that_redeclares_inherited_properties : Specification
{
    JsonSchema _schema;
    JsonSchema _caseDifferingSchema;
    BsonDocument _stored;
    BsonDocument _caseDifferingStored;
    Exception _error;
    Exception _caseDifferingError;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "type": "object",
              "allOf": [{"$ref": "#/$defs/Base"}],
              "properties": {"amount": {"type": "number", "format": "decimal"}},
              "$defs": {
                "Base": {
                  "type": "object",
                  "properties": {"amount": {"type": "number", "format": "double"}}
                }
              }
            }
            """);
        _caseDifferingSchema = JsonSchema.FromJson("""
            {
              "type": "object",
              "allOf": [{"$ref": "#/$defs/Base"}],
              "properties": {"amount": {"type": "number", "format": "decimal"}},
              "$defs": {
                "Base": {
                  "type": "object",
                  "properties": {"Amount": {"type": "number", "format": "double"}}
                }
              }
            }
            """);
    }

    void Because()
    {
        _error = Catch.Exception(() => _stored = EventContentBson.FromJson("""{"amount":3.75}""", _schema));
        _caseDifferingError = Catch.Exception(() => _caseDifferingStored = EventContentBson.FromJson("""{"amount":3.75}""", _caseDifferingSchema));
    }

    [Fact] void should_not_throw_for_a_redeclared_property() => _error.ShouldBeNull();
    [Fact] void should_use_the_siblings_decimal_format() => _stored["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_not_throw_for_a_case_differing_property() => _caseDifferingError.ShouldBeNull();
    [Fact] void should_use_the_siblings_decimal_format_when_case_differs() => _caseDifferingStored["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
}
