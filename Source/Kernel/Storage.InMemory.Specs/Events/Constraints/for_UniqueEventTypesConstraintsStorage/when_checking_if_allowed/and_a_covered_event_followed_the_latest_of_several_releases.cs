// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// The cycle ends at the most recent release across every declared removal event, not at the most recent of any one
/// of them. Taking the latest of a single removal event would place the boundary before the loan that is currently
/// open and let a second one through — the constraint would look released while it is not.
/// </summary>
public class and_a_covered_event_followed_the_latest_of_several_releases : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _returnedEventType, _borrower);
        await Append(2, _checkedOutEventType, _borrower);
        await Append(3, _writtenOffEventType, _borrower);

        // The cycle that is open now, started after every release the constraint declares.
        await Append(4, _checkedOutEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturnOrWriteOff, _borrower);

    [Fact] void should_not_be_allowed() => _isAllowed.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_that_holds_the_open_cycle() => _sequenceNumber.ShouldEqual((EventSequenceNumber)4U);
}
