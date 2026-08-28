// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_FacetSetGenerator.when_generating;

public class from_three_facets : Specification
{
    FacetSet _source;
    IEnumerable<FacetSet> _result;

    void Establish() => _source = new FacetSet(
    [
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.Day, "Monday"),
        new Facet(FacetName.TimeBucket, "Morning")
    ]);

    void Because() => _result = new FacetSetGenerator().Generate(_source, 3);

    [Fact] void should_produce_every_non_empty_combination() => _result.Count().ShouldEqual(7);
    [Fact] void should_produce_each_single_facet() => _result.Count(_ => _.Specificity == 1).ShouldEqual(3);
    [Fact] void should_produce_each_pair() => _result.Count(_ => _.Specificity == 2).ShouldEqual(3);
    [Fact] void should_produce_the_full_combination() => _result.Count(_ => _.Specificity == 3).ShouldEqual(1);
    [Fact] void should_produce_distinct_combinations() => _result.Select(_ => _.Key).Distinct().Count().ShouldEqual(7);
    [Fact] void should_not_produce_the_empty_combination() => _result.Any(_ => _.IsEmpty).ShouldBeFalse();
}
