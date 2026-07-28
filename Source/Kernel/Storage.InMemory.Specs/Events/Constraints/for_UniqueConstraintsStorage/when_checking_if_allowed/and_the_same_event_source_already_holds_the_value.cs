// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// Re-claiming a value an event source already holds is allowed, and reports the sequence number the value was
/// claimed at - the same answer MongoDB and SQL give for a hit on the caller's own entry.
/// </summary>
public class and_the_same_event_source_already_holds_the_value : given.a_unique_constraints_storage
{
    static readonly EventSourceId _holder = "first-source";
    static readonly UniqueConstraintValue _value = "some-value";
    static readonly EventSequenceNumber _claimedAt = 42UL;
    bool _isAllowed;
    EventSequenceNumber _sequenceNumber;

    async Task Because()
    {
        await _storage.Save(_holder, ConstraintNameValue, _claimedAt, _value);
        (_isAllowed, _sequenceNumber) = await _storage.IsAllowed(_holder, _definition, _value);
    }

    [Fact] void should_be_allowed() => _isAllowed.ShouldBeTrue();
    [Fact] void should_report_the_sequence_number_it_was_claimed_at() => _sequenceNumber.ShouldEqual(_claimedAt);
}
