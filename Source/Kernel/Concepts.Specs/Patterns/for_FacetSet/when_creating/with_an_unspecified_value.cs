// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_creating;

public class with_an_unspecified_value : Specification
{
    FacetSet _result;

    void Because() => _result = new FacetSet(
    [
        new Facet(FacetName.CommandType, "ApproveExpenseReport"),
        new Facet(FacetName.CausedByCommand, FacetValue.Unspecified)
    ]);

    [Fact] void should_drop_the_facet_without_a_value() => _result.Specificity.ShouldEqual(1);
    [Fact] void should_keep_the_facet_with_a_value() => _result.Constrains(FacetName.CommandType).ShouldBeTrue();
    [Fact] void should_not_constrain_the_dropped_facet() => _result.Constrains(FacetName.CausedByCommand).ShouldBeFalse();
}
