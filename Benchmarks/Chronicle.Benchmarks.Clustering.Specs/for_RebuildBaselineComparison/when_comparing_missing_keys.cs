// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_missing_keys : Specification
{
    IReadOnlyList<string> _differences = null!;
    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string> { ["a"] = "{}" }, new Dictionary<string, string> { ["b"] = "{}" });
    [Fact] void should_report_both_key_differences() => _differences.Count.ShouldEqual(2);
}
