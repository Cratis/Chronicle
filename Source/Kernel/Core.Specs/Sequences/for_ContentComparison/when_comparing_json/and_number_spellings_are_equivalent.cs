// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_ContentComparison.when_comparing_json;

public class and_number_spellings_are_equivalent : Specification
{
    bool _result;

    void Because() => _result = ContentComparison.Equals(JsonNode.Parse("[1.00,-0.0,1234567890123456789012345678901e-2]"), JsonNode.Parse("[1e0,0,12345678901234567890123456789.01]"));

    [Fact] void should_compare_numbers_without_rounding_or_a_clr_precision_limit() => _result.ShouldBeTrue();
}
