// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_claimants_race : given.a_v2_store
{
    Result<bool, UniqueConstraintWriteFailure>[] _results;
    int _claimCount;
    bool _winnerOwns;

    async Task Establish() => (await _storage.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        using var barrier = new Barrier(3);
        var first = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await _storage.Save(_source, _definition, 10, _value, _scope, _authority);
        });
        var second = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await _storage.Save(_otherSource, _definition, 11, _value, _scope, _authority);
        });
        barrier.SignalAndWait();
        _results = await Task.WhenAll(first, second);
        _claimCount = (await _storage.GetFor(_source, _definition, _scope)).Count() + (await _storage.GetFor(_otherSource, _definition, _scope)).Count();
        _winnerOwns = (await _storage.IsAllowed(_results[0].IsSuccess ? _source : _otherSource, _definition, _value, _scope)).IsAllowed;
    }

    [Fact] void should_accept_exactly_one_claim() => _results.Count(_ => _.IsSuccess).ShouldEqual(1);
    [Fact] void should_refuse_the_conflict() => _results.Single(_ => !_.IsSuccess).AsT1.ShouldEqual(UniqueConstraintWriteFailure.ConflictingClaim);
    [Fact] void should_store_only_the_winner() => (_claimCount == 1 && _winnerOwns).ShouldBeTrue();
}
