// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_holder_releases_after_a_rejected_claim : given.a_claimed_value
{
    async Task Establish() => await Catch.Exception(() => _storage.Save(_otherOwner, _definition, 43UL, _value));

    async Task Because() => await _storage.Remove(_owner, _definition);

    [Fact] async Task should_release_the_value_for_the_original_owner() => (await _storage.IsAllowed(_owner, _definition, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_value_for_another_source() => (await _storage.IsAllowed(EventSourceId.New(), _definition, _value)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_have_no_claim_position() => (await _storage.IsAllowed(_otherOwner, _definition, _value)).SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
