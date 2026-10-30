// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_releasing_as_the_owner;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    SingleWriterLeaseAttempt _result;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    async Task Because()
    {
        await Storage.Release(Claim.Owner, CancellationToken.None);
        _result = await Storage.TryAcquire(Claim with { Owner = new("owner-b") }, CancellationToken.None);
    }

    [Fact] void should_admit_the_successor_immediately() => _result.Acquired.ShouldBeTrue();
}
