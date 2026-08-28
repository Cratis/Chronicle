// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_BehaviorPattern.when_matching;

public class a_context_that_constrains_more_than_the_pattern : Specification
{
    BehaviorPattern _pattern;
    FacetSet _matchingContext;
    FacetSet _disagreeingContext;
    FacetSet _lessConstrainedContext;

    void Establish()
    {
        _pattern = new BehaviorPattern(
            "user-42",
            new FacetSet([new Facet(FacetName.Day, "Monday")]),
            10,
            0.8d,
            0.2d,
            5d,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        _matchingContext = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _disagreeingContext = new FacetSet([new Facet(FacetName.Day, "Tuesday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _lessConstrainedContext = new FacetSet([new Facet(FacetName.TimeBucket, "Morning")]);
    }

    [Fact] void should_match_a_context_that_agrees_and_says_more() => _pattern.Matches(_matchingContext).ShouldBeTrue();
    [Fact] void should_not_match_a_context_that_disagrees() => _pattern.Matches(_disagreeingContext).ShouldBeFalse();
    [Fact] void should_not_match_a_context_that_leaves_its_facet_open() => _pattern.Matches(_lessConstrainedContext).ShouldBeFalse();
    [Fact] void should_report_its_specificity() => _pattern.Specificity.ShouldEqual(1);
}
