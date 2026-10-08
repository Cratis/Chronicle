// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_ContentComparison.when_comparing_json;

public class and_digits_beyond_decimal_precision_differ : Specification
{
    bool _result;

    void Because() => _result = ContentComparison.Equals(JsonNode.Parse("12345678901234567890123456789.01"), JsonNode.Parse("12345678901234567890123456789.02"));

    [Fact] void should_not_round_distinct_numbers_to_equality() => _result.ShouldBeFalse();
}
