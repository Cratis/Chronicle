// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_declaring_a_removal_event;

/// <summary>
/// Declarations sharing a name merge into one constraint, and one constraint has one removal event. It is declared on
/// whichever declaration the author reached it from, so the merge coalesces rather than keeping the first
/// declaration's value — otherwise a removal event declared on the second declaration is silently dropped and the
/// constraint goes back to meaning "forever" with nothing reporting it.
/// </summary>
public class and_several_event_types_share_a_name : given.a_constraint_builder_with_owner
{
    const string ConstraintName = "LoanOpen";

    IImmutableList<IConstraintDefinition> _result;
    EventType _checkedOutEventType;
    EventType _renewedEventType;
    EventType _returnedEventType;

    void Establish()
    {
        _checkedOutEventType = new EventType(nameof(LoanCheckedOut), EventTypeGeneration.First);
        _renewedEventType = new EventType(nameof(LoanRenewed), EventTypeGeneration.First);
        _returnedEventType = new EventType(nameof(LoanReturned), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(LoanCheckedOut)).Returns(_checkedOutEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanRenewed)).Returns(_renewedEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanReturned)).Returns(_returnedEventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<LoanCheckedOut>(name: ConstraintName);
        _constraintBuilder.Unique<LoanRenewed>(name: ConstraintName).RemovedWith<LoanReturned>();
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_merge_into_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_cover_both_event_types() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_checkedOutEventType.Id, _renewedEventType.Id]);
    [Fact] void should_keep_the_removal_event() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldEqual(_returnedEventType.Id);

    record LoanCheckedOut();
    record LoanRenewed();
    record LoanReturned();
}
