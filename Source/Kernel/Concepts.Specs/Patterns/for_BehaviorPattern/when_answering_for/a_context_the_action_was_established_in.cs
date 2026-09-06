// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_BehaviorPattern.when_answering_for;

/// <summary>
/// The case the feature exists for, and the one plain subset matching cannot serve. A caller asking what somebody
/// usually does on a Monday early morning names the day and the time and nothing else, because the command is the
/// thing they are asking for. Requiring the whole pattern to be a subset of that question excludes precisely the
/// pattern that answers it.
/// </summary>
public class a_context_the_action_was_established_in : Specification
{
    BehaviorPattern _registersInvoices;
    FacetSet _theMomentAsked;
    FacetSet _anotherDay;
    FacetSet _theDayAlone;

    void Establish()
    {
        _registersInvoices = new BehaviorPattern(
            "ingrid.holm",
            new FacetSet(
            [
                new Facet(FacetName.CommandType, "RegisterInvoice"),
                new Facet(FacetName.Day, "Monday"),
                new Facet(FacetName.TimeBucket, "EarlyMorning")
            ]),
            730,
            1d,
            0.2d,
            5d,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        _theMomentAsked = new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "EarlyMorning")]);
        _anotherDay = new FacetSet([new Facet(FacetName.Day, "Friday"), new Facet(FacetName.TimeBucket, "EarlyMorning")]);
        _theDayAlone = new FacetSet([new Facet(FacetName.Day, "Monday")]);
    }

    [Fact] void should_answer_the_moment_it_was_established_in() => _registersInvoices.AnswersFor(_theMomentAsked).ShouldBeTrue();
    [Fact] void should_not_answer_a_moment_it_disagrees_with() => _registersInvoices.AnswersFor(_anotherDay).ShouldBeFalse();
    [Fact] void should_not_answer_a_context_leaving_one_of_its_facets_open() => _registersInvoices.AnswersFor(_theDayAlone).ShouldBeFalse();
    [Fact] void should_name_the_action_it_answers_with() => _registersInvoices.Action.ShouldEqual(new FacetValue("RegisterInvoice"));
    [Fact] void should_report_how_much_of_the_question_it_uses() => _registersInvoices.ContextSpecificity.ShouldEqual(2);

    [Fact] void should_still_not_match_that_moment_as_a_description() => _registersInvoices.Matches(_theMomentAsked).ShouldBeFalse();
}
