// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_reading_a_decimal_value_without_a_schema : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(
        new JsonObject { ["amount"] = JsonValue.Create(193.58m), ["precise"] = JsonValue.Create(1234567890.123456789012345678m) },
        new JsonSchema());

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)_result["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_result["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
