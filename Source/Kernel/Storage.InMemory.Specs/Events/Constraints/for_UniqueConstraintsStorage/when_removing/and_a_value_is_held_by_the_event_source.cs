// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_removing;

/// <summary>
/// Removing an event source's claim releases whatever value it held for that constraint and scope.
/// </summary>
public class and_a_value_is_held_by_the_event_source : given.a_unique_constraints_storage
{
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";
    static readonly UniqueConstraintValue _value = "some-value";
    bool _isAllowedForOther;

    async Task Because()
    {
        await _storage.Save(_holder, ConstraintNameValue, EventSequenceNumber.First, _value);
        await _storage.Remove(_holder, ConstraintNameValue);
        (_isAllowedForOther, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _value);
    }

    [Fact] void should_allow_another_event_source_to_claim_the_value() => _isAllowedForOther.ShouldBeTrue();
}
