// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// An event source holds at most one value per constraint and scope, so saving a new value releases the one it held.
/// MongoDB and SQL get this from replacing the single document/row keyed by event source; the in-memory index must
/// behave the same or it reports a violation against a value nobody claims anymore.
/// </summary>
public class and_the_holder_has_moved_on_to_another_value : given.a_unique_constraints_storage
{
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";
    static readonly UniqueConstraintValue _releasedValue = "released-value";
    static readonly UniqueConstraintValue _currentValue = "current-value";
    bool _isAllowedToClaimReleasedValue;
    bool _isAllowedToClaimCurrentValue;

    async Task Because()
    {
        await _storage.Save(_holder, ConstraintNameValue, EventSequenceNumber.First, _releasedValue);
        await _storage.Save(_holder, ConstraintNameValue, EventSequenceNumber.First + 1, _currentValue);
        (_isAllowedToClaimReleasedValue, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _releasedValue);
        (_isAllowedToClaimCurrentValue, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _currentValue);
    }

    [Fact] void should_allow_another_event_source_to_claim_the_released_value() => _isAllowedToClaimReleasedValue.ShouldBeTrue();
    [Fact] void should_not_allow_another_event_source_to_claim_the_current_value() => _isAllowedToClaimCurrentValue.ShouldBeFalse();
}
