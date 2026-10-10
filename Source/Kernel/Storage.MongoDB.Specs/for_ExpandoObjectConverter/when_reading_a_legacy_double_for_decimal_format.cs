// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_reading_a_legacy_double_for_decimal_format : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(new BsonDocument { ["amount"] = new BsonDouble(193.58) },
        JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}"""));

    [Fact] void should_use_shortest_round_trip_text() => decimal.GetBits((decimal)_result["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
}
