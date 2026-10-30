// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_two_owners_race;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    int _winners;

    async Task Because()
    {
        var results = await Task.WhenAll(
            Task.Run(() => Storage.TryAcquire(Claim, CancellationToken.None)),
            Task.Run(() => Storage.TryAcquire(Claim with { Owner = new("owner-b") }, CancellationToken.None)));
        _winners = results.Count(result => result.Acquired);
    }

    [Fact] void should_admit_exactly_one_owner() => _winners.ShouldEqual(1);
}
