// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_replacing_a_source_value : given.a_seeded_v2_store
{
    async Task Because() => (await _storage.Save(_source, _definition, 20, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
    [Fact] async Task should_release_the_old_value() => (await _storage.IsAllowed(_otherSource, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_only_the_new_value() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_otherValue]);
    [Fact] async Task should_keep_the_new_originating_sequence() => (await _storage.IsAllowed(_source, _definition, _otherValue, _scope)).SequenceNumber.Value.ShouldEqual(20UL);
}
