// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_value_removal_is_not_applicable : given.a_seeded_v2_store
{
    async Task Because()
    {
        (await _storage.RemoveValue(_source, _definition, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveValue(_otherSource, _perValue, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Remove(_otherSource, _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
    }

    [Fact] async Task should_preserve_the_claim_in_per_source_mode_or_for_another_owner() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
