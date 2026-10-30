// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_changed_values : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string> { ["key"] = """{"value":2}""" }, new Dictionary<string, string> { ["key"] = """{"value":1}""" });

    [Fact] void should_report_the_changed_key() => _differences.Single().ShouldEqual("Changed value at key: key");
}
