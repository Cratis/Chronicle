// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_adding_a_facet;

public class that_is_already_constrained : Specification
{
    FacetSet _original;
    FacetSet _result;

    void Establish() => _original = new FacetSet([new Facet(FacetName.Day, "Monday")]);

    void Because() => _result = _original.With(FacetName.Day, "Tuesday");

    [Fact] void should_replace_the_value() => _result.ValueOf(FacetName.Day).Value.ShouldEqual("Tuesday");
    [Fact] void should_not_grow_the_set() => _result.Specificity.ShouldEqual(1);
    [Fact] void should_leave_the_original_untouched() => _original.ValueOf(FacetName.Day).Value.ShouldEqual("Monday");
}
