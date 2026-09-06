// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching_actions;

/// <summary>
/// An empty answer is a true statement - this scope does nothing established here - and the best of a bad set reads
/// to a caller exactly like a real habit. Nothing is promoted to fill the gap.
/// </summary>
public class a_context_nothing_clears_the_bar_for : Specification
{
    BehaviorPattern _tooUncertain;
    FacetSet _context;
    IEnumerable<BehaviorPattern> _result;
    IEnumerable<BehaviorPattern> _resultWithoutRoom;

    void Establish()
    {
        _tooUncertain = Pattern([new Facet(FacetName.CommandType, "RegisterInvoice"), Monday], confidence: 0.2d);
        _context = new FacetSet([Monday]);
    }

    void Because()
    {
        var matcher = new PatternMatcher();
        _result = matcher.MatchActions([_tooUncertain], _context, new PatternConfidence(0.5d), 10);
        _resultWithoutRoom = matcher.MatchActions([_tooUncertain], _context, PatternConfidence.None, 0);
    }

    [Fact] void should_answer_with_nothing() => _result.ShouldBeEmpty();
    [Fact] void should_answer_with_nothing_when_asked_for_no_results() => _resultWithoutRoom.ShouldBeEmpty();

    static Facet Monday => new(FacetName.Day, "Monday");

    static BehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new("ingrid.holm", new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
