// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_equal_values : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string> { ["key"] = """{"value":1}""" }, new Dictionary<string, string> { ["key"] = """{"value":1}""" });

    [Fact] void should_preserve_value_semantics() => (_differences.Count == 0).ShouldEqual(true);
}
