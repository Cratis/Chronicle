// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_creating;

public class with_the_same_facet_name_twice : Specification
{
    FacetSet _result;

    void Because() => _result = new FacetSet(
    [
        new Facet(FacetName.Day, "Monday"),
        new Facet(FacetName.Day, "Tuesday")
    ]);

    [Fact] void should_keep_only_one_value_for_the_name() => _result.Specificity.ShouldEqual(1);
    [Fact] void should_keep_the_first_one_seen() => _result.ValueOf(FacetName.Day).Value.ShouldEqual("Monday");
}
