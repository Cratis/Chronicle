// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson.when_parsing_recursive_content;

public class with_decimals : Specification
{
    BsonDocument _empty;
    BsonDocument _finite;

    void Because()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"features":{"type":"array","items":{"type":"object","properties":{"amount":{"type":"number","format":"decimal?"},"subFeatures":{"type":"array","items":{"$ref":"#/properties/features/items"}}}}}}}""");
        _empty = EventContentBson.FromJson("{}", schema);
        _finite = EventContentBson.FromJson("""{"features":[{"amount":193.58,"subFeatures":[{"amount":193.58,"subFeatures":[]}]}]}""", schema);
    }

    [Fact] void should_preserve_empty_content() => _empty.ElementCount.ShouldEqual(0);
    [Fact] void should_convert_the_top_level_decimal() => _finite["features"][0]["amount"].AsDecimal128.ShouldEqual(new Decimal128(193.58m));
    [Fact] void should_convert_the_nested_decimal() => _finite["features"][0]["subFeatures"][0]["amount"].AsDecimal128.ShouldEqual(new Decimal128(193.58m));
}
