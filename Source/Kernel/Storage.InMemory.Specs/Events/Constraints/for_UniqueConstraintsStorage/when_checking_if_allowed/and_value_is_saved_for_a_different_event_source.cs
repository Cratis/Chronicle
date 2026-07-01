// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

public class and_value_is_saved_for_a_different_event_source : given.a_unique_constraints_storage
{
    static readonly EventSourceId _savedEventSourceId = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";
    static readonly UniqueConstraintValue _value = "some-value";
    bool _isAllowedForOther;
    bool _isAllowedForSame;

    async Task Because()
    {
        await _storage.Save(_savedEventSourceId, ConstraintNameValue, EventSequenceNumber.First, _value);
        (_isAllowedForOther, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _value);
        (_isAllowedForSame, _) = await _storage.IsAllowed(_savedEventSourceId, _definition, _value);
    }

    [Fact] void should_not_allow_a_different_event_source() => _isAllowedForOther.ShouldBeFalse();
    [Fact] void should_allow_the_same_event_source() => _isAllowedForSame.ShouldBeTrue();
}
