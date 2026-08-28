// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_FacetSetGenerator.when_generating;

public class from_nothing_to_combine : Specification
{
    FacetSetGenerator _generator;
    FacetSet _oneFacet;

    void Establish()
    {
        _generator = new();
        _oneFacet = new FacetSet([new Facet(FacetName.Day, "Monday")]);
    }

    [Fact] void should_produce_nothing_from_an_empty_set() => _generator.Generate(FacetSet.Empty, 3).ShouldBeEmpty();
    [Fact] void should_produce_nothing_for_a_zero_cap() => _generator.Generate(_oneFacet, 0).ShouldBeEmpty();
    [Fact] void should_produce_nothing_for_a_negative_cap() => _generator.Generate(_oneFacet, -1).ShouldBeEmpty();
    [Fact] void should_produce_the_single_facet_when_the_cap_is_larger_than_the_set() => _generator.Generate(_oneFacet, 3).Count().ShouldEqual(1);
}
