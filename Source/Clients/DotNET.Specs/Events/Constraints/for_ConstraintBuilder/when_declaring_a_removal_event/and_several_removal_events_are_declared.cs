// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_declaring_a_removal_event;

/// <summary>
/// The unique-event-type form has the same shape as the value form: a cycle can end in more than one way, so the
/// chain accepts a removal declaration for each. Each call used to replace the previous one on the definition it
/// applies to, so a loan released by being returned or written off only ever released on whichever was written last.
/// </summary>
public class and_several_removal_events_are_declared : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;
    EventType _checkedOutEventType;
    EventType _returnedEventType;
    EventType _writtenOffEventType;

    void Establish()
    {
        _checkedOutEventType = new EventType(nameof(LoanCheckedOut), EventTypeGeneration.First);
        _returnedEventType = new EventType(nameof(LoanReturned), EventTypeGeneration.First);
        _writtenOffEventType = new EventType(nameof(LoanWrittenOff), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(LoanCheckedOut)).Returns(_checkedOutEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanReturned)).Returns(_returnedEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanWrittenOff)).Returns(_writtenOffEventType);
    }

    void Because()
    {
        _constraintBuilder
            .Unique<LoanCheckedOut>(name: "LoanOpen")
            .RemovedWith<LoanReturned>()
            .RemovedWith<LoanWrittenOff>();

        _result = _constraintBuilder.Build();
    }

    [Fact] void should_have_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_still_cover_the_declared_event_type() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_checkedOutEventType.Id]);
    [Fact] void should_carry_every_declared_removal_event() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldContainOnly([_returnedEventType.Id, _writtenOffEventType.Id]);

    record LoanCheckedOut();
    record LoanReturned();
    record LoanWrittenOff();
}
