// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_a_source_moves_onto_a_held_value : given.a_claimed_value
{
    Exception? _error;

    async Task Establish() => await _storage.Save(_otherOwner, _definition, 43UL, "other-value");

    async Task Because() => _error = await Catch.Exception(() => _storage.Save(_owner, _definition, 44UL, "other-value"));

    [Fact] void should_report_a_constraint_collision() => _error.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] async Task should_keep_the_old_claim_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_old_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).SequenceNumber.ShouldEqual(_sequenceNumber);
    [Fact] async Task should_keep_the_held_value_reserved() => (await _storage.IsAllowed(_owner, _definition, "other-value")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_held_claim_position() => (await _storage.IsAllowed(_owner, _definition, "other-value")).SequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
