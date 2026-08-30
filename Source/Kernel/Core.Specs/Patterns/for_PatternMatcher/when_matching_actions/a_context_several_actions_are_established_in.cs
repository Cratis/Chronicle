// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMatcher.when_matching_actions;

/// <summary>
/// Confidence leads when ranking answers, where specificity leads when ranking descriptions. Confidence is already
/// the chance of the action given the context it was established in, which compares directly between one answer and
/// the next; a facet count does not.
/// </summary>
public class a_context_several_actions_are_established_in : Specification
{
    BehaviorPattern _almostAlways;
    BehaviorPattern _sometimes;
    BehaviorPattern _onAnotherDay;
    BehaviorPattern _theMomentItself;
    FacetSet _context;
    IEnumerable<BehaviorPattern> _result;

    void Establish()
    {
        _almostAlways = Pattern([Action("RegisterInvoice"), Monday, EarlyMorning], confidence: 0.95d);
        _sometimes = Pattern([Action("MatchInvoice"), Monday], confidence: 0.4d);
        _onAnotherDay = Pattern([Action("ReleasePayment"), new Facet(FacetName.Day, "Friday")], confidence: 1d);
        _theMomentItself = Pattern([EarlyMorning], confidence: 0.53d);

        _context = new FacetSet([Monday, EarlyMorning]);
    }

    void Because() => _result = new PatternMatcher().MatchActions(
        [_almostAlways, _sometimes, _onAnotherDay, _theMomentItself],
        _context,
        PatternConfidence.None,
        10);

    [Fact] void should_answer_with_the_actions_established_here() => _result.Count().ShouldEqual(2);
    [Fact] void should_put_the_most_likely_action_first() => _result.First().ShouldEqual(_almostAlways);
    [Fact] void should_keep_the_less_likely_action() => _result.Last().ShouldEqual(_sometimes);
    [Fact] void should_not_answer_with_an_action_from_another_context() => _result.ShouldNotContain(_onAnotherDay);
    [Fact] void should_not_hand_the_question_back_as_an_answer() => _result.ShouldNotContain(_theMomentItself);

    static Facet Monday => new(FacetName.Day, "Monday");
    static Facet EarlyMorning => new(FacetName.TimeBucket, "EarlyMorning");
    static Facet Action(string command) => new(FacetName.CommandType, command);

    static BehaviorPattern Pattern(IEnumerable<Facet> facets, double confidence) =>
        new("ingrid.holm", new FacetSet(facets), 10, confidence, 0.5d, 1d, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
}
