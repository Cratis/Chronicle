// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_decimal_content_across_all_of_branches : Specification
{
    JsonSchema _schema;
    BsonDocument _stored;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "type": "object",
          "allOf": [
            {"type": "object", "properties": {"first": {"type": "number", "format": "decimal"}}},
            {"type": "object", "properties": {"second": {"type": "number", "format": "decimal"}}}
          ],
          "properties": {"own": {"type": "number", "format": "decimal"}}
        }
        """);

    void Because() => _stored = EventContentBson.FromJson("""{"first":3.75,"second":3.75,"own":3.75}""", _schema);

    [Fact] void should_store_the_first_branch_decimal_as_decimal128() => _stored["first"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_second_branch_decimal_as_decimal128() => _stored["second"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_sibling_decimal_as_decimal128() => _stored["own"].BsonType.ShouldEqual(BsonType.Decimal128);
}
