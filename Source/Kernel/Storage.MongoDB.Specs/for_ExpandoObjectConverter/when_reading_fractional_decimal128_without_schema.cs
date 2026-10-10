// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_reading_fractional_decimal128_without_schema : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(
        new BsonDocument { ["amount"] = new BsonDecimal128(193.58m), ["precise"] = new BsonDecimal128(1234567890.123456789012345678m) },
        JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null},"precise":{"default":null}}}"""));

    [Fact] void should_not_truncate_the_fraction() => decimal.GetBits((decimal)_result["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_result["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
