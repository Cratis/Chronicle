// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_FacetSetGenerator.when_generating;

/// <summary>
/// The cap is what keeps the candidate space polynomial rather than exponential in the number of facets, so it has
/// to actually bind: four facets capped at two is ten combinations, not fifteen.
/// </summary>
public class with_a_cap_below_the_facet_count : Specification
{
    FacetSet _source;
    IEnumerable<FacetSet> _result;

    void Establish() => _source = new FacetSet(
    [
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.Day, "Monday"),
        new Facet(FacetName.TimeBucket, "Morning"),
        new Facet(FacetName.AggregateType, "ExpenseReport")
    ]);

    void Because() => _result = new FacetSetGenerator().Generate(_source, 2);

    [Fact] void should_produce_only_combinations_up_to_the_cap() => _result.Count().ShouldEqual(10);
    [Fact] void should_not_produce_anything_above_the_cap() => _result.Any(_ => _.Specificity > 2).ShouldBeFalse();
}
