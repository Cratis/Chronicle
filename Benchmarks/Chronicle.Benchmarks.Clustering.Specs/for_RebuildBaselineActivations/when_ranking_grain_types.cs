// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineActivations;

public class when_ranking_grain_types : Specification
{
    RebuildBaselineActivations _report = null!;
    void Because() => _report = RebuildBaselineActivations.From(Enumerable.Range(1, 16)
        .Select(count => ($"grain-{count}", count)).Append(("grain-16", 1)));
    [Fact] void should_include_all_types_in_the_total() => _report.TotalActivations.ShouldEqual(137L);
    [Fact] void should_limit_the_breakdown_to_fifteen_types() => _report.ActivationsByGrainType.Count.ShouldEqual(15);
    [Fact] void should_exclude_the_least_numerous_type() => _report.ActivationsByGrainType.ContainsKey("grain-1").ShouldBeFalse();
    [Fact] void should_aggregate_duplicate_types() => _report.ActivationsByGrainType["grain-16"].ShouldEqual(17L);
    [Fact] void should_rank_by_count_descending() => _report.ActivationsByGrainType.Keys.First().ShouldEqual("grain-16");
}
