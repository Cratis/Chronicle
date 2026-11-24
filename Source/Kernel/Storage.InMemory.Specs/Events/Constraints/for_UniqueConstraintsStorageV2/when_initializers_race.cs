// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_initializers_race : given.a_v2_store
{
    Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>[] _results;

    async Task Because()
    {
        using var barrier = new Barrier(3);
        var first = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await _storage.InitializeAuthority(_definition.Name, _authority);
        });
        var second = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await _storage.InitializeAuthority(_definition.Name, _authority with { Phase = Run(1) });
        });
        barrier.SignalAndWait();
        _results = await Task.WhenAll(first, second);
    }

    [Fact] void should_initialize_exactly_once() => _results.Count(_ => _.IsSuccess).ShouldEqual(1);
    [Fact] void should_refuse_the_loser() => _results.Single(_ => !_.IsSuccess).AsT1.ShouldEqual(UniqueConstraintWriteFailure.AuthorityMismatch);
    [Fact] async Task should_store_the_winning_proof() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_results.Single(_ => _.IsSuccess).AsT0);
}
