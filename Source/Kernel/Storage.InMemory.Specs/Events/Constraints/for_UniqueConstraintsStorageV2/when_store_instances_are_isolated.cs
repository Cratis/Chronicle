// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_store_instances_are_isolated : given.a_seeded_v2_store
{
    UniqueConstraintsStorageV2 _other;

    async Task Because()
    {
        _other = new();
        var legacy = new UniqueConstraintsStorage();
        await legacy.Save(_otherSource, _definition, 12, _value, _scope.Value);
    }

    [Fact] async Task should_not_share_authority_across_instances() => (await _other.GetAuthority(_definition.Name)).HasValue.ShouldBeFalse();
    [Fact] async Task should_not_share_claims_across_instances() => (await _other.GetFor(_source, _definition, _scope)).ShouldBeEmpty();
    [Fact] async Task should_not_share_v1_claims() => (await _storage.IsAllowed(_source, _definition, _value, _scope)).SequenceNumber.Value.ShouldEqual(10UL);
}
