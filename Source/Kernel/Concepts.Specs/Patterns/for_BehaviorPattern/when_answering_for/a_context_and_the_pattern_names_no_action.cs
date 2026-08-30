// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_BehaviorPattern.when_answering_for;

/// <summary>
/// A pattern of pure context establishes that a situation recurs, not what is done in it. Letting it through as an
/// answer is how a caller ends up being told "you are asking about an early morning" at some confidence, which is
/// their own question handed back to them.
/// </summary>
public class a_context_and_the_pattern_names_no_action : Specification
{
    BehaviorPattern _theMomentItself;
    FacetSet _theMomentAsked;

    void Establish()
    {
        _theMomentItself = new BehaviorPattern(
            "ingrid.holm",
            new FacetSet([new Facet(FacetName.TimeBucket, "EarlyMorning")]),
            1400,
            0.53d,
            0.53d,
            5d,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        _theMomentAsked = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "EarlyMorning")]);
    }

    [Fact] void should_not_answer_what_usually_happens() => _theMomentItself.AnswersFor(_theMomentAsked).ShouldBeFalse();
    [Fact] void should_still_describe_the_context() => _theMomentItself.Matches(_theMomentAsked).ShouldBeTrue();
    [Fact] void should_name_no_action() => _theMomentItself.Action.ShouldEqual(FacetValue.Unspecified);
    [Fact] void should_count_all_of_its_facets_as_context() => _theMomentItself.ContextSpecificity.ShouldEqual(1);
}
