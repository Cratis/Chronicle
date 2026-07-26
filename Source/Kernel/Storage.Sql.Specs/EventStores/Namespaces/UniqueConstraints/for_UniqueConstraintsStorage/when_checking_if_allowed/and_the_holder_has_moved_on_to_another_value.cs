// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// An event source holds at most one value per constraint and scope, so saving a new value releases the one it held.
/// This pins the behavior every <see cref="Cratis.Chronicle.Storage.Events.Constraints.IUniqueConstraintsStorage"/>
/// implementation has to agree on.
/// </summary>
public class and_the_holder_has_moved_on_to_another_value : given.a_unique_constraints_storage
{
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";
    static readonly UniqueConstraintValue _releasedValue = "released-value";
    static readonly UniqueConstraintValue _currentValue = "current-value";
    static readonly EventSequenceNumber _claimedAt = 42UL;

    bool _isAllowedToClaimReleasedValue;
    bool _isAllowedToClaimCurrentValue;
    bool _isAllowedForHolder;
    EventSequenceNumber _sequenceNumberForHolder;

    async Task Because()
    {
        await _storage.Save(_holder, ConstraintNameValue, EventSequenceNumber.First, _releasedValue);
        await _storage.Save(_holder, ConstraintNameValue, _claimedAt, _currentValue);

        (_isAllowedToClaimReleasedValue, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _releasedValue);
        (_isAllowedToClaimCurrentValue, _) = await _storage.IsAllowed(_otherEventSourceId, _definition, _currentValue);
        (_isAllowedForHolder, _sequenceNumberForHolder) = await _storage.IsAllowed(_holder, _definition, _currentValue);
    }

    [Fact] void should_allow_another_event_source_to_claim_the_released_value() => _isAllowedToClaimReleasedValue.ShouldBeTrue();
    [Fact] void should_not_allow_another_event_source_to_claim_the_current_value() => _isAllowedToClaimCurrentValue.ShouldBeFalse();
    [Fact] void should_allow_the_holder_to_reclaim_its_current_value() => _isAllowedForHolder.ShouldBeTrue();
    [Fact] void should_report_the_sequence_number_the_holder_claimed_at() => _sequenceNumberForHolder.ShouldEqual(_claimedAt);
}
