// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_two_sources_claim_the_same_value : given.a_claimed_value
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => _storage.Save(_otherOwner, _definition, 43UL, _value));

    [Fact] void should_report_a_constraint_collision() => _error.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] async Task should_keep_the_original_owner() => (await _storage.IsAllowed(_owner, _definition, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_refuse_the_other_owner() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_original_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).SequenceNumber.ShouldEqual(_sequenceNumber);
}
