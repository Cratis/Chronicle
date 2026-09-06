// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching;

public class with_a_result_limit : Specification
{
    PatternMatcher _matcher;
    BehaviorPattern[] _patterns;
    FacetSet _context;

    void Establish()
    {
        _matcher = new();
        _context = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
        _patterns =
        [
            Pattern([new Facet(FacetName.Day, "Monday")]),
            Pattern([new Facet(FacetName.TimeBucket, "Morning")]),
            Pattern([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")])
        ];
    }

    [Fact] void should_return_at_most_the_limit() => _matcher.Match(_patterns, _context, PatternConfidence.None, 2).Count().ShouldEqual(2);
    [Fact] void should_keep_the_most_specific_within_the_limit() => _matcher.Match(_patterns, _context, PatternConfidence.None, 1).Single().Specificity.ShouldEqual(2);
    [Fact] void should_return_nothing_for_a_limit_of_zero() => _matcher.Match(_patterns, _context, PatternConfidence.None, 0).ShouldBeEmpty();
    [Fact] void should_return_nothing_for_a_negative_limit() => _matcher.Match(_patterns, _context, PatternConfidence.None, -1).ShouldBeEmpty();

    static BehaviorPattern Pattern(IEnumerable<Facet> facets) =>
        new("user-42", new FacetSet(facets), 10, 0.9d, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
