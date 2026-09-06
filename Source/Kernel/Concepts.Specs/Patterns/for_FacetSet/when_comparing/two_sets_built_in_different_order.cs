// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_comparing;

public class two_sets_built_in_different_order : Specification
{
    FacetSet _first;
    FacetSet _second;

    void Establish()
    {
        _first = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _second = new FacetSet([new Facet(FacetName.TimeBucket, "Morning"), new Facet(FacetName.Day, "Monday")]);
    }

    [Fact] void should_consider_them_equal() => _first.ShouldEqual(_second);
    [Fact] void should_give_them_the_same_key() => _first.Key.ShouldEqual(_second.Key);
    [Fact] void should_give_them_the_same_hash_code() => _first.GetHashCode().ShouldEqual(_second.GetHashCode());
}
