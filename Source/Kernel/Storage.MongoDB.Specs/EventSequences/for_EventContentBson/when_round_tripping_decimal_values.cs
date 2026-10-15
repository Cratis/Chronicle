// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_round_tripping_decimal_values : Specification
{
    BsonDocument _document;
    System.Text.Json.Nodes.JsonObject _read;

    void Because()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","$defs":{"Amount":{"type":"number","format":"decimal"}},"properties":{"amount":{"type":"number","format":"decimal"},"precise":{"$ref":"#/$defs/Amount"},"values":{"type":"array","items":{"$ref":"#/$defs/Amount"}},"ratio":{"type":"number","format":"double"}}}""");
        _document = EventContentBson.FromJson("""{"amount":193.58,"precise":1234567890.123456789012345678,"values":[193.58],"ratio":193.58}""", schema);
        _read = EventContentBson.ToJsonObject(_document);
    }

    [Fact] void should_store_the_amount_as_decimal128() => _document["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_reference_as_decimal128() => _document["precise"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_store_the_array_element_as_decimal128() => _document["values"][0].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_amount_bits() => decimal.GetBits(_read["amount"]!.GetValue<decimal>()).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_digits() => decimal.GetBits(_read["precise"]!.GetValue<decimal>()).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
    [Fact] void should_leave_doubles_as_doubles() => _document["ratio"].BsonType.ShouldEqual(BsonType.Double);
}
