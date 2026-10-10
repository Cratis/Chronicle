// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_a_double_for_a_decimal_format : Specification
{
    decimal _result;

    void Because()
    {
        dynamic content = new ExpandoObject();
        content.amount = 193.58d;
        _result = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject((ExpandoObject)content,
            JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}"""))["amount"]!.GetValue<decimal>();
    }

    [Fact] void should_preserve_the_shortest_representation() => decimal.GetBits(_result).ShouldEqual(decimal.GetBits(193.58m));
}
