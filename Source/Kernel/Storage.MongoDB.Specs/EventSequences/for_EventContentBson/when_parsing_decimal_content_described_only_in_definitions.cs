// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_decimal_content_described_only_in_definitions : Specification
{
    BsonDocument _definitions;
    BsonDocument _defs;

    void Because()
    {
        _definitions = EventContentBson.FromJson("""{"amount":193.58}""", JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"$ref":"#/definitions/amount"}},"definitions":{"amount":{"type":"number","format":"decimal"}}}"""));
        _defs = EventContentBson.FromJson("""{"amount":193.58}""", JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"$ref":"#/$defs/amount"}},"$defs":{"amount":{"type":"number","format":"decimal?"}}}"""));
    }

    [Fact] void should_convert_decimals_in_definitions() => _definitions["amount"].AsDecimal128.ShouldEqual(new Decimal128(193.58m));
    [Fact] void should_convert_decimals_in_defs() => _defs["amount"].AsDecimal128.ShouldEqual(new Decimal128(193.58m));
}
