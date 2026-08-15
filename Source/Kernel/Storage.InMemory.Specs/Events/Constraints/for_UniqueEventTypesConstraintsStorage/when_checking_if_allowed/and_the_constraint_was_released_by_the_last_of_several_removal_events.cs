// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A cycle can end in more than one way, so each declared removal event ends it. Answering against only one of them
/// would keep the event source blocked after it reached a terminal fact the constraint itself declares — the loan was
/// written off, the borrower is free, and the next loan would still be refused.
/// </summary>
public class and_the_constraint_was_released_by_the_last_of_several_removal_events : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _writtenOffEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturnOrWriteOff, _borrower);

    [Fact] void should_be_allowed() => _isAllowed.ShouldBeTrue();
    [Fact] void should_have_no_sequence_number_to_report() => _sequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
