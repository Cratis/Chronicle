// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_nullable_decimal_content : Specification
{
    BsonDocument _stored;

    void Because() => _stored = EventContentBson.FromJson("""{"amount":193.58,"absent":null,"nested":{"amount":193.58},"values":[193.58,null]}""",
        JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"oneOf":[{"type":"null"},{"type":"number","format":"decimal"}]},"absent":{"oneOf":[{"type":"null"},{"type":"number","format":"decimal"}]},"nested":{"oneOf":[{"type":"null"},{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}]},"values":{"type":"array","items":{"oneOf":[{"type":"null"},{"type":"number","format":"decimal"}]}}}}"""));

    [Fact] void should_store_nullable_decimals_as_decimal128() => _stored["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_nulls() => _stored["absent"].BsonType.ShouldEqual(BsonType.Null);
    [Fact] void should_descend_into_nullable_objects() => _stored["nested"]["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_descend_into_nullable_array_items() => _stored["values"][0].BsonType.ShouldEqual(BsonType.Decimal128);
}
