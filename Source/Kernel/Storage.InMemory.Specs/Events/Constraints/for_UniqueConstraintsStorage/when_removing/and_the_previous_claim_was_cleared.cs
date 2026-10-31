// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_removing;

public class and_the_previous_claim_was_cleared : given.a_claimed_value
{
    async Task Establish()
    {
        await _storage.ClearValues(_definition);
        await _storage.Save(_otherOwner, _definition, 43UL, _value);
    }

    async Task Because() => await _storage.Remove(_owner, _definition);

    [Fact] async Task should_keep_the_new_owners_claim_reserved() => (await _storage.IsAllowed(_owner, _definition, _value)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_new_claim_position() => (await _storage.IsAllowed(_owner, _definition, _value)).SequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
