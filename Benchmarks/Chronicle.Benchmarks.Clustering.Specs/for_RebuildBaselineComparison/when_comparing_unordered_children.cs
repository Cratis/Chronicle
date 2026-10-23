// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_unordered_children : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string> { ["key"] = """{"children":[{"id":"b"},{"id":"a"}]}""" }, new Dictionary<string, string> { ["key"] = """{"children":[{"id":"a"},{"id":"b"}]}""" });

    [Fact] void should_preserve_value_semantics() => (_differences.Count == 0).ShouldEqual(true);
}
