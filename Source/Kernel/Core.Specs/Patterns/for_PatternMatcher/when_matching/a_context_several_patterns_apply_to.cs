// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching;

/// <summary>
/// Specificity outranks confidence. A pattern that constrains everything the caller asked about answers their
/// question; a broader, more confident one answers a question they did not ask, and putting it first is how a
/// recommendation ends up stating the obvious.
/// </summary>
public class a_context_several_patterns_apply_to : Specification
{
    BehaviorPattern _broadAndCertain;
    BehaviorPattern _specific;
    BehaviorPattern _elsewhere;
    FacetSet _context;
    IEnumerable<BehaviorPattern> _result;

    void Establish()
    {
        _broadAndCertain = Pattern([new Facet(FacetName.Day, "Monday")], confidence: 1d);
        _specific = Pattern([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")], confidence: 0.7d);
        _elsewhere = Pattern([new Facet(FacetName.Day, "Friday")], confidence: 1d);

        _context = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")]);
    }

    void Because() => _result = new PatternMatcher().Match(
        [_broadAndCertain, _specific, _elsewhere],
        _context,
        PatternConfidence.None,
        10);

    [Fact] void should_return_only_the_patterns_that_apply() => _result.Count().ShouldEqual(2);
    [Fact] void should_rank_the_most_specific_first() => _result.First().ShouldEqual(_specific);
    [Fact] void should_rank_the_broader_one_after_it() => _result.Last().ShouldEqual(_broadAndCertain);
    [Fact] void should_not_return_a_pattern_for_another_context() => _result.ShouldNotContain(_elsewhere);

    static BehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new("user-42", new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
