// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_declaring_a_removal_event;

/// <summary>
/// The removal seam the per-event-source form was missing. Without it the constraint can only say "at most one,
/// forever" — expressible, but wrong for any lifecycle that repeats, and there was no way to say anything else.
/// </summary>
public class and_a_unique_event_type_constraint_was_declared : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;
    EventType _checkedOutEventType;
    EventType _returnedEventType;

    void Establish()
    {
        _checkedOutEventType = new EventType(nameof(LoanCheckedOut), EventTypeGeneration.First);
        _returnedEventType = new EventType(nameof(LoanReturned), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(LoanCheckedOut)).Returns(_checkedOutEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanReturned)).Returns(_returnedEventType);
    }

    void Because()
    {
        _constraintBuilder
            .Unique<LoanCheckedOut>(name: "LoanOpen")
            .RemovedWith<LoanReturned>();

        _result = _constraintBuilder.Build();
    }

    [Fact] void should_have_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_still_cover_the_declared_event_type() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_checkedOutEventType.Id]);
    [Fact] void should_carry_the_removal_event() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldContainOnly([_returnedEventType.Id]);

    record LoanCheckedOut();
    record LoanReturned();
}
