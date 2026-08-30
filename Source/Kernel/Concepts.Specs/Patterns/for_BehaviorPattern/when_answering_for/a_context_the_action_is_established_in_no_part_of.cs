// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_BehaviorPattern.when_answering_for;

/// <summary>
/// An action established in no context at all is a base rate for the whole scope - "half of what this person ever
/// does is post ledger entries" - and its empty context is a subset of every question there is. Left alone it
/// answers a question about a Sunday night the scope has never worked with the same 52% it answers everything
/// else, which makes "nothing is established here" unsayable for anybody who has a dominant action.
/// </summary>
public class a_context_the_action_is_established_in_no_part_of : Specification
{
    BehaviorPattern _whatTheyMostlyDo;
    FacetSet _aMomentTheyNeverWork;
    FacetSet _noQuestionAtAll;

    void Establish()
    {
        _whatTheyMostlyDo = new BehaviorPattern(
            "petter.aas",
            new FacetSet([new Facet(FacetName.CommandType, "PostLedgerEntry")]),
            649,
            0.52d,
            0.52d,
            5d,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        _aMomentTheyNeverWork = new FacetSet([new Facet(FacetName.Day, "Sunday"), new Facet(FacetName.TimeBucket, "Night")]);
        _noQuestionAtAll = FacetSet.Empty;
    }

    [Fact] void should_not_answer_a_question_it_uses_no_part_of() => _whatTheyMostlyDo.AnswersFor(_aMomentTheyNeverWork).ShouldBeFalse();
    [Fact] void should_answer_when_nothing_was_asked() => _whatTheyMostlyDo.AnswersFor(_noQuestionAtAll).ShouldBeTrue();
    [Fact] void should_be_conditioned_on_nothing() => _whatTheyMostlyDo.ContextSpecificity.ShouldEqual(0);
}
