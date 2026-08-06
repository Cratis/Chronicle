// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A release opens the next cycle, it does not disable the constraint. The second checkout holds the current cycle,
/// so a third is refused — and against that event's sequence number, not the one from the cycle already closed.
/// Answering "has a covered event ever been appended" or "is there a release at all" would both get this wrong.
/// </summary>
public class and_a_covered_event_followed_the_release : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _returnedEventType, _borrower);
        await Append(2, _checkedOutEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);

    [Fact] void should_not_be_allowed() => _isAllowed.ShouldBeFalse();
    [Fact] void should_report_the_sequence_number_from_the_current_cycle() => _sequenceNumber.ShouldEqual((EventSequenceNumber)2U);
}
