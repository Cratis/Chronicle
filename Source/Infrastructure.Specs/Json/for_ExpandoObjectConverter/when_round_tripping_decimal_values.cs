// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_decimal_values : Specification
{
    IDictionary<string, object?> _result;
    readonly decimal _precise = 1234567890.123456789012345678m;

    void Because()
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"},"unformatted":{"type":"number"}}}""");
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.precise = _precise;
        content.unformatted = _precise;
        var json = converter.ToJsonObject((ExpandoObject)content, schema);
        _result = converter.ToExpandoObject(JsonNode.Parse(json.ToJsonString())!.AsObject(), schema);
        _result["unformatted"] = json["unformatted"]!.GetValue<decimal>();
    }

    [Fact] void should_preserve_the_amount_bits() => decimal.GetBits((decimal)_result["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_result["precise"]!).ShouldEqual(decimal.GetBits(_precise));
    [Fact] void should_not_narrow_a_decimal_without_a_format() => decimal.GetBits((decimal)_result["unformatted"]!).ShouldEqual(decimal.GetBits(_precise));
}
