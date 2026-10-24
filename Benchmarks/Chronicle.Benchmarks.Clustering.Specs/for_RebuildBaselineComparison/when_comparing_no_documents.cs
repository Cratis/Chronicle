// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineComparison;

public class when_comparing_no_documents : Specification
{
    IReadOnlyList<string> _differences = null!;

    void Because() => _differences = RebuildBaselineComparison.Compare(new Dictionary<string, string>(), new Dictionary<string, string>());

    [Fact] void should_reject_a_vacuous_comparison() => _differences.Single().ShouldEqual("No documents compared.");
}
