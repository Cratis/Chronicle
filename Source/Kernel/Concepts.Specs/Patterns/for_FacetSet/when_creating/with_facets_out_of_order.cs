// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_creating;

public class with_facets_out_of_order : Specification
{
    FacetSet _result;

    void Because() => _result = new FacetSet(
    [
        new Facet(FacetName.TimeBucket, "Morning"),
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.Day, "Monday")
    ]);

    [Fact] void should_hold_every_facet() => _result.Specificity.ShouldEqual(3);
    [Fact] void should_order_facets_by_name() => _result.Facets.Select(_ => _.Name.Value).ToArray().ShouldEqual(["CommandType", "Day", "TimeBucket"]);
    [Fact] void should_build_the_key_in_the_same_order() => _result.Key.Value.ShouldEqual("CommandType=ApproveExpenseReport;Day=Monday;TimeBucket=Morning");
}
