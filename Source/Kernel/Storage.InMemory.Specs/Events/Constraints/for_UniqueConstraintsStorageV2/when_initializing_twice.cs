// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_initializing_twice : given.a_seeded_v2_store
{
    Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure> _result;
    async Task Because() => _result = await _storage.InitializeAuthority(_definition.Name, _authority with { Phase = Run(1) });
    [Fact] void should_refuse_replacement() => _result.AsT1.ShouldEqual(UniqueConstraintWriteFailure.AuthorityMismatch);
    [Fact] async Task should_keep_the_original_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    [Fact] async Task should_keep_existing_claims() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
