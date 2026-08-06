// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintDefinition.when_converting_to_contract;

/// <summary>
/// The definition has carried a removal event all along and the conversion dropped it, so the kernel never saw one
/// and enforced "at most one, forever" against a client that had declared otherwise. Nothing reported a problem: the
/// value was discarded client-side, before the kernel was involved, and the constraint went on working — just not
/// releasing.
/// </summary>
public class with_a_removal_event : Specification
{
    static readonly ConstraintName _constraintName = "LoanOpen";

    UniqueEventTypeConstraintDefinition _definition;
    EventType _checkedOutEventType;
    EventType _returnedEventType;
    Constraint _contract;

    void Establish()
    {
        _checkedOutEventType = new EventType("LoanCheckedOut", EventTypeGeneration.First);
        _returnedEventType = new EventType("LoanReturned", EventTypeGeneration.First);
        _definition = new UniqueEventTypeConstraintDefinition(
            _constraintName,
            _ => string.Empty,
            [_checkedOutEventType.Id],
            _returnedEventType.Id);
    }

    void Because() => _contract = _definition.ToContract();

    [Fact] void should_carry_the_removal_event() => _contract.RemovedWith.ShouldEqual(_returnedEventType.Id.Value);
}
