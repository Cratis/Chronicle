// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public abstract partial class UniqueConstraintsStorageV2SqlContract
{
    [Fact]
    public async Task should_reuse_the_same_proof_for_all_mutations_at_the_revision_ceiling()
    {
        var authority = _authority with { Revision = new(long.MaxValue) };
        (await _storage.InitializeAuthority(_definition.Name, authority)).IsSuccess.ShouldBeTrue();
        for (var operation = 0; operation < 6; operation++)
        {
            (await Mutation(operation, authority)).IsSuccess.ShouldBeTrue();
            (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)authority);
        }

        _storage = await ReopenStorage();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)authority);
        ShouldRefuse(await _storage.TryTransition(_definition.Name, authority, Run(1)), UniqueConstraintWriteFailure.RevisionExhausted);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)authority);
    }
}
