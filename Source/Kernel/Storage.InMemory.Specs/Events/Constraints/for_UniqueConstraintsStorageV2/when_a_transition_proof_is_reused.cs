// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_a_transition_proof_is_reused : given.a_seeded_v2_store
{
    UniqueConstraintWriteAuthority _next;
    Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure> _result;
    async Task Establish() => _next = (await _storage.TryTransition(_definition.Name, _authority, Run(1))).AsT0;
    async Task Because() => _result = await _storage.TryTransition(_definition.Name, _authority, Run(2));
    [Fact] void should_refuse_the_old_proof() => _result.AsT1.ShouldEqual(UniqueConstraintWriteFailure.AuthorityMismatch);
    [Fact] void should_advance_only_once() => _next.Revision.Value.ShouldEqual(1L);
    [Fact] void should_preserve_the_store_epoch() => _next.Epoch.ShouldEqual(_authority.Epoch);
    [Fact] async Task should_leave_claims_untouched() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
