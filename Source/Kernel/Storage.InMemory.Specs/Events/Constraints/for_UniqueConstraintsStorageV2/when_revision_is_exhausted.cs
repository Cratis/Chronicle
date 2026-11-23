// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_revision_is_exhausted : given.a_v2_store
{
    Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure> _result;
    async Task Establish()
    {
        _authority = _authority with { Revision = new(long.MaxValue) };
        await Seed();
    }

    async Task Because() => _result = await _storage.TryTransition(_definition.Name, _authority, Run(1));
    [Fact] void should_refuse_without_wraparound() => _result.AsT1.ShouldEqual(UniqueConstraintWriteFailure.RevisionExhausted);
    [Fact] async Task should_keep_the_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    [Fact] async Task should_keep_the_claim() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
