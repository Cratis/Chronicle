// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching_actions;

/// <summary>
/// The empty answer has to survive contact with a scope that has a dominant action. Asking about a moment nobody
/// works must come back with nothing rather than with what the scope does the rest of the week, or "no established
/// behavior here" stops being something the surface can say.
/// </summary>
public class a_context_only_a_scope_wide_habit_covers : Specification
{
    BehaviorPattern _whatTheyMostlyDo;
    BehaviorPattern _whatTheyDoOnFridays;
    IEnumerable<BehaviorPattern> _atAMomentTheyNeverWork;
    IEnumerable<BehaviorPattern> _whenNothingIsAsked;

    void Establish()
    {
        _whatTheyMostlyDo = Pattern([Action("PostLedgerEntry")], confidence: 0.52d);
        _whatTheyDoOnFridays = Pattern([Action("ClosePeriod"), new Facet(FacetName.Day, "Friday")], confidence: 0.8d);
    }

    void Because()
    {
        var matcher = new PatternMatcher();
        BehaviorPattern[] held = [_whatTheyMostlyDo, _whatTheyDoOnFridays];

        _atAMomentTheyNeverWork = matcher.MatchActions(
            held,
            new FacetSet([new Facet(FacetName.Day, "Sunday"), new Facet(FacetName.TimeBucket, "Night")]),
            PatternConfidence.None,
            10);

        _whenNothingIsAsked = matcher.MatchActions(held, FacetSet.Empty, PatternConfidence.None, 10);
    }

    [Fact] void should_answer_nothing_for_a_moment_no_habit_covers() => _atAMomentTheyNeverWork.ShouldBeEmpty();
    [Fact] void should_answer_with_the_general_habit_when_nothing_is_asked() => _whenNothingIsAsked.ShouldContain(_whatTheyMostlyDo);
    [Fact] void should_not_answer_with_a_narrower_habit_when_nothing_is_asked() => _whenNothingIsAsked.ShouldNotContain(_whatTheyDoOnFridays);

    static Facet Action(string command) => new(FacetName.CommandType, command);

    static BehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new("petter.aas", new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
