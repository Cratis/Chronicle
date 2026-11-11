// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_ContentComparison.when_comparing_json;

public class and_exponent_markers_differ_in_case : Specification
{
    bool _result;

    void Because() => _result = ContentComparison.Equals(JsonNode.Parse("[1E2,1.5E-1,25e1,7]"), JsonNode.Parse("[100,0.15,2.5E2,7e0]"));

    [Fact] void should_treat_upper_and_lower_case_exponent_markers_alike() => _result.ShouldBeTrue();
}
