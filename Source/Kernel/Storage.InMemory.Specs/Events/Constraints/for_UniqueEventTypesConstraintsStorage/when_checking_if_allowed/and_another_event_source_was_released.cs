// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// The constraint is per event source, so a release belongs to the event source that recorded it. Reading the most
/// recent removal event across the whole sequence instead would let any borrower's return open everybody else's
/// next cycle - the constraint would then be released by unrelated traffic rather than by the lifecycle it guards.
/// </summary>
public class and_another_event_source_was_released : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _returnedEventType, _anotherBorrower);
    }

    async Task Because() => (_isAllowed, _) = await _storage.IsAllowed(DefinitionReleasedByReturn, _borrower);

    [Fact] void should_not_be_allowed() => _isAllowed.ShouldBeFalse();
}
