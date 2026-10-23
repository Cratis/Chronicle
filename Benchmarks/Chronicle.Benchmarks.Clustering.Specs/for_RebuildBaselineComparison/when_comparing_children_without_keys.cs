// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_children_without_keys : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(
        new Dictionary<string, string> { ["key"] = """{"children":[{"name":"child"}]}""" },
        new Dictionary<string, string> { ["key"] = """{"children":[{"name":"child"}]}""" });

    [Fact] void should_report_the_missing_child_key() => _differences.Single().ShouldEqual("Invalid document at key: key: Unordered collection 'children' contains an element without an _id/id/Id key.");
}
