// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_checking_subset;

public class against_a_more_constrained_set : Specification
{
    FacetSet _narrow;
    FacetSet _wide;
    FacetSet _disagreeing;

    void Establish()
    {
        _narrow = new FacetSet([new Facet(FacetName.Day, "Monday")]);
        _wide = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _disagreeing = new FacetSet([new Facet(FacetName.Day, "Tuesday"), new Facet(FacetName.TimeBucket, "Morning")]);
    }

    [Fact] void should_consider_the_narrow_set_a_subset_of_the_wide_one() => _narrow.IsSubsetOf(_wide).ShouldBeTrue();
    [Fact] void should_not_consider_the_wide_set_a_subset_of_the_narrow_one() => _wide.IsSubsetOf(_narrow).ShouldBeFalse();
    [Fact] void should_not_consider_a_set_with_a_different_value_a_subset() => _narrow.IsSubsetOf(_disagreeing).ShouldBeFalse();
    [Fact] void should_consider_the_empty_set_a_subset_of_anything() => FacetSet.Empty.IsSubsetOf(_wide).ShouldBeTrue();
}
