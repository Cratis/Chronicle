// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_non_decimal_content_with_a_schema : Specification
{
    const string Content = """{"amount":193.58,"nested":{"count":7},"values":[1,null],"large":18446744073709551615}""";
    BsonDocument _fast;
    BsonDocument _slow;
    BsonDocument _cached;
    BsonDocument _withoutSchema;

    void Because()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number"},"nested":{"type":"object","properties":{"count":{"type":"integer"}}},"values":{"type":"array","items":{"type":"integer"}},"large":{"type":"integer"}}}""");
        var decimalSchema = JsonSchema.FromJson("""{"type":"object","properties":{"unused":{"type":"number","format":"decimal"}}}""");
        _fast = EventContentBson.FromJson(Content, schema);
        _cached = EventContentBson.FromJson(Content, schema);
        _slow = EventContentBson.FromJson(Content, decimalSchema);
        _withoutSchema = EventContentBson.FromJson(Content);
    }

    [Fact] void should_produce_the_same_bson_as_the_schema_decimal_path() => _fast.ShouldEqual(_slow);
    [Fact] void should_produce_the_same_bson_as_the_original_reader_path() => _fast.ShouldEqual(_withoutSchema);
    [Fact] void should_preserve_the_cached_path() => _cached.ShouldEqual(_fast);
}
