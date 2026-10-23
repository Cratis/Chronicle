// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_renewing_with_another_owner;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    bool _renewed;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    async Task Because()
    {
        _renewed = await Storage.Renew(new("owner-a"), Claim.Ttl, CancellationToken.None);
    }

    [Fact] void should_refuse_renewal() => _renewed.ShouldBeFalse();
}
