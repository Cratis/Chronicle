// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_ordered_arrays : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string> { ["key"] = """{"ordered":[2,1]}""" }, new Dictionary<string, string> { ["key"] = """{"ordered":[1,2]}""" });

    [Fact] void should_report_the_changed_key() => _differences.Single().ShouldEqual("Changed value at key: key");
}
