// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_declaring_a_removal_event;

/// <summary>
/// Declarations sharing a name merge into one constraint, and the removal events declared across them are unioned
/// rather than picked from one. Keeping only one would silently drop the release the author wrote next to the other
/// declaration, and the constraint would go on blocking a cycle that had ended.
/// </summary>
public class and_several_event_types_share_a_name_and_each_declares_one : given.a_constraint_builder_with_owner
{
    const string ConstraintName = "LoanOpen";

    IImmutableList<IConstraintDefinition> _result;
    EventType _checkedOutEventType;
    EventType _renewedEventType;
    EventType _returnedEventType;
    EventType _writtenOffEventType;

    void Establish()
    {
        _checkedOutEventType = new EventType(nameof(LoanCheckedOut), EventTypeGeneration.First);
        _renewedEventType = new EventType(nameof(LoanRenewed), EventTypeGeneration.First);
        _returnedEventType = new EventType(nameof(LoanReturned), EventTypeGeneration.First);
        _writtenOffEventType = new EventType(nameof(LoanWrittenOff), EventTypeGeneration.First);
        _eventTypes.GetEventTypeFor(typeof(LoanCheckedOut)).Returns(_checkedOutEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanRenewed)).Returns(_renewedEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanReturned)).Returns(_returnedEventType);
        _eventTypes.GetEventTypeFor(typeof(LoanWrittenOff)).Returns(_writtenOffEventType);
    }

    void Because()
    {
        _constraintBuilder.Unique<LoanCheckedOut>(name: ConstraintName).RemovedWith<LoanReturned>();
        _constraintBuilder.Unique<LoanRenewed>(name: ConstraintName).RemovedWith<LoanWrittenOff>();
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_merge_into_a_single_constraint() => _result.Count.ShouldEqual(1);
    [Fact] void should_cover_both_event_types() => ((UniqueEventTypeConstraintDefinition)_result[0]).EventTypeIds.ShouldContainOnly([_checkedOutEventType.Id, _renewedEventType.Id]);
    [Fact] void should_keep_every_declared_removal_event() => ((UniqueEventTypeConstraintDefinition)_result[0]).RemovedWith.ShouldContainOnly([_returnedEventType.Id, _writtenOffEventType.Id]);

    record LoanCheckedOut();
    record LoanRenewed();
    record LoanReturned();
    record LoanWrittenOff();
}
