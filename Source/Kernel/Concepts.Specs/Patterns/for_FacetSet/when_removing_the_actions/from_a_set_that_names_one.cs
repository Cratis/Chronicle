// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_removing_the_actions;

/// <summary>
/// Splitting a mined itemset into the action and the context it was taken in is what both mining and answering are
/// built on - confidence is the frequency of the whole over the frequency of this remainder, and an answer matches
/// on this remainder rather than on the whole.
/// </summary>
public class from_a_set_that_names_one : Specification
{
    FacetSet _withAnAction;
    FacetSet _pureContext;
    FacetSet _actionOnly;

    FacetSet _contextOfTheFirst;
    FacetSet _contextOfTheSecond;
    FacetSet _contextOfTheThird;

    void Establish()
    {
        _withAnAction = new FacetSet(
        [
            new Facet(FacetName.CommandType, "RegisterInvoice"),
            new Facet(FacetName.Day, "Monday"),
            new Facet(FacetName.TimeBucket, "EarlyMorning")
        ]);

        _pureContext = new FacetSet([new Facet(FacetName.Day, "Monday")]);
        _actionOnly = new FacetSet([new Facet(FacetName.CommandType, "RegisterInvoice")]);
    }

    void Because()
    {
        _contextOfTheFirst = _withAnAction.WithoutActions();
        _contextOfTheSecond = _pureContext.WithoutActions();
        _contextOfTheThird = _actionOnly.WithoutActions();
    }

    [Fact] void should_drop_the_action() => _contextOfTheFirst.ConstrainsAction.ShouldBeFalse();
    [Fact] void should_keep_every_context_facet() => _contextOfTheFirst.Specificity.ShouldEqual(2);
    [Fact] void should_keep_the_context_values() => _contextOfTheFirst.ValueOf(FacetName.Day).ShouldEqual(new FacetValue("Monday"));
    [Fact] void should_leave_a_set_naming_no_action_alone() => _contextOfTheSecond.ShouldEqual(_pureContext);
    [Fact] void should_reduce_a_set_that_is_only_an_action_to_nothing() => _contextOfTheThird.IsEmpty.ShouldBeTrue();

    [Fact] void should_report_that_the_first_names_an_action() => _withAnAction.ConstrainsAction.ShouldBeTrue();
    [Fact] void should_read_the_action_off_the_first() => _withAnAction.Action.ShouldEqual(new FacetValue("RegisterInvoice"));
    [Fact] void should_report_no_action_for_pure_context() => _pureContext.Action.ShouldEqual(FacetValue.Unspecified);
}
