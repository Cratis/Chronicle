// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// The unchanged base case: nothing has released the constraint, so the second covered event is refused and the
/// sequence number of the one already holding the cycle comes back for the violation to point at.
/// </summary>
public class and_a_covered_event_was_already_appended : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish() => await Append(0, _checkedOutEventType, _borrower);

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);

    [Fact] void should_not_be_allowed() => _isAllowed.ShouldBeFalse();
    [Fact] void should_report_the_sequence_number_of_the_event_holding_the_cycle() => _sequenceNumber.ShouldEqual((EventSequenceNumber)0U);
}
