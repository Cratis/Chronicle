// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_clearing_one_mode_of_the_same_constraint : given.a_seeded_v2_store
{
    async Task Establish() => (await _storage.Save(_otherSource, _perValue, 12, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();

    async Task Because() => (await _storage.RemoveAllFor(_perValue, _authority)).IsSuccess.ShouldBeTrue();

    [Fact] async Task should_preserve_per_event_source_claims() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_remove_per_value_claims() => (await _storage.GetFor(_otherSource, _perValue, _scope)).ShouldBeEmpty();
    [Fact] async Task should_keep_the_per_event_source_value_reserved() => (await _storage.IsAllowed(_otherSource, _definition, _value, _scope)).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_release_only_the_per_value_reservation() => (await _storage.IsAllowed(_otherSource, _perValue, _value, _scope)).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_share_constraint_authority_between_modes() => (await _storage.GetAuthority(_perValue.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
}
