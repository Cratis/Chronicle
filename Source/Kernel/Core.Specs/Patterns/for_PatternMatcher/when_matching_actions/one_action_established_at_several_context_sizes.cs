// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching_actions;

/// <summary>
/// A habit is mined at every context size at once - on a Monday, in the early morning, and on a Monday early
/// morning are three itemsets describing one behavior. Returning all three says the same thing three times and
/// pushes the second-most-likely action out of a limited result set, so one survives: the one conditioned on most
/// of the question, which is the best-informed of the three even when a broader sibling reads as more confident.
/// </summary>
public class one_action_established_at_several_context_sizes : Specification
{
    BehaviorPattern _onTheWholeDay;
    BehaviorPattern _atThatTimeOfDay;
    BehaviorPattern _atThatMoment;
    BehaviorPattern _aSecondAction;
    FacetSet _context;
    IEnumerable<BehaviorPattern> _result;

    void Establish()
    {
        _onTheWholeDay = Pattern([Action("RegisterInvoice"), Monday], confidence: 0.99d);
        _atThatTimeOfDay = Pattern([Action("RegisterInvoice"), EarlyMorning], confidence: 0.90d);
        _atThatMoment = Pattern([Action("RegisterInvoice"), Monday, EarlyMorning], confidence: 0.80d);
        _aSecondAction = Pattern([Action("MatchInvoice"), Monday, EarlyMorning], confidence: 0.20d);

        _context = new FacetSet([Monday, EarlyMorning]);
    }

    void Because() => _result = new PatternMatcher().MatchActions(
        [_onTheWholeDay, _atThatTimeOfDay, _atThatMoment, _aSecondAction],
        _context,
        PatternConfidence.None,
        10);

    [Fact] void should_answer_once_per_action() => _result.Count().ShouldEqual(2);
    [Fact] void should_keep_the_answer_conditioned_on_most_of_the_question() => _result.ShouldContain(_atThatMoment);
    [Fact] void should_drop_the_broader_sibling_even_though_it_reads_more_confident() => _result.ShouldNotContain(_onTheWholeDay);
    [Fact] void should_drop_the_other_broader_sibling() => _result.ShouldNotContain(_atThatTimeOfDay);
    [Fact] void should_still_rank_the_surviving_answers_by_confidence() => _result.First().ShouldEqual(_atThatMoment);
    [Fact] void should_keep_the_second_action_that_would_otherwise_be_crowded_out() => _result.Last().ShouldEqual(_aSecondAction);

    static Facet Monday => new(FacetName.Day, "Monday");
    static Facet EarlyMorning => new(FacetName.TimeBucket, "EarlyMorning");
    static Facet Action(string command) => new(FacetName.CommandType, command);

    static BehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new("ingrid.holm", new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
