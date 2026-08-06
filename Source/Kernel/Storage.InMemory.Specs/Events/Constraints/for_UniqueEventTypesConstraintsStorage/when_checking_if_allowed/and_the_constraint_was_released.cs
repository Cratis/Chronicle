// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// The whole point of declaring a removal event: the loan was returned, so the next one may start. Without this the
/// constraint can only say "at most one, forever", which every lifecycle that repeats runs into exactly once and
/// then never recovers from.
/// </summary>
public class and_the_constraint_was_released : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _returnedEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);

    [Fact] void should_be_allowed() => _isAllowed.ShouldBeTrue();
    [Fact] void should_have_no_sequence_number_to_report() => _sequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
