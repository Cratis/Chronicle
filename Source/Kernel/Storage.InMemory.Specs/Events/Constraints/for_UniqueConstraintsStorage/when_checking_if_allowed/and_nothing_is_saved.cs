// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

public class and_nothing_is_saved : given.a_unique_constraints_storage
{
    static readonly EventSourceId _eventSourceId = "some-source";
    static readonly UniqueConstraintValue _value = "some-value";
    bool _isAllowed;

    async Task Because() => (_isAllowed, _) = await _storage.IsAllowed(_eventSourceId, _definition, _value);

    [Fact] void should_be_allowed() => _isAllowed.ShouldBeTrue();
}
