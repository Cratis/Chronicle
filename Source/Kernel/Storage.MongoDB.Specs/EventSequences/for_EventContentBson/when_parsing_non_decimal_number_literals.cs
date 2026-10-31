// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_non_decimal_number_literals : Specification
{
    const string Content = """{"amount":0.10000000000000001,"large":18446744073709551615}""";
    BsonDocument _fast;
    BsonDocument _slow;

    void Because()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"double"},"large":{"type":"integer"}}}""");
        var decimalSchema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"double"},"large":{"type":"integer"},"unused":{"type":"number","format":"decimal"}}}""");
        _fast = EventContentBson.FromJson(Content, schema);
        _slow = EventContentBson.FromJson(Content, decimalSchema);
    }

    [Fact] void should_match_the_schema_decimal_path() => _fast.ShouldEqual(_slow);
    [Fact] void should_leave_non_decimal_numbers_as_double() => _fast["amount"].BsonType.ShouldEqual(BsonType.Double);
    [Fact] void should_preserve_large_unsigned_integers() => _fast["large"].BsonType.ShouldEqual(BsonType.Decimal128);
}
