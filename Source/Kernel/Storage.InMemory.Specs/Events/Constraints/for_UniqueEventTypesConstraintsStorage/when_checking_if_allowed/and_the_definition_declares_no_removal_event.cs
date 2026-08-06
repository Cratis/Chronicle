// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// Releasing is opt-in. An event that happens to end the lifecycle in the domain releases nothing unless the
/// constraint names it, so a definition without a removal event keeps meaning "at most one, forever" — the
/// behavior every constraint declared before this existed relies on.
/// </summary>
public class and_the_definition_declares_no_removal_event : given.a_unique_event_types_constraints_storage
{
    bool _isAllowed;

    async Task Establish()
    {
        await Append(0, _checkedOutEventType, _borrower);
        await Append(1, _returnedEventType, _borrower);
    }

    async Task Because() => (_isAllowed, _) = await _storage.IsAllowed(DefinitionWithoutRemovalEvent, _borrower);

    [Fact] void should_not_be_allowed() => _isAllowed.ShouldBeFalse();
}
