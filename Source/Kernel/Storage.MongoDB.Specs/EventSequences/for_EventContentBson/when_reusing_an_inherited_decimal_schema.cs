// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_reusing_an_inherited_decimal_schema : Specification
{
    BsonDocument _first;
    BsonDocument _second;

    void Because()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","allOf":[{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}],"properties":{"nested":{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}},"values":{"type":"array","items":{"type":"number","format":"decimal"}}}}""");
        _first = EventContentBson.FromJson("""{"AMOUNT":1.25,"nested":{"AMOUNT":2.5},"values":[3.75,null]}""", schema);
        _second = EventContentBson.FromJson("""{"AMOUNT":4.25,"nested":{"AMOUNT":5.5},"values":[6.75,null]}""", schema);
    }

    [Fact] void should_resolve_inherited_properties_case_insensitively() => _first["AMOUNT"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_reuse_schema_metadata_without_reusing_values() => _second["AMOUNT"].AsDecimal128.ShouldEqual(new Decimal128(4.25m));
    [Fact] void should_preserve_nested_case_insensitive_properties() => _second["nested"]["AMOUNT"].AsDecimal128.ShouldEqual(new Decimal128(5.5m));
    [Fact] void should_preserve_array_decimals() => _second["values"][0].AsDecimal128.ShouldEqual(new Decimal128(6.75m));
    [Fact] void should_preserve_array_nulls() => _second["values"][1].BsonType.ShouldEqual(BsonType.Null);
}
