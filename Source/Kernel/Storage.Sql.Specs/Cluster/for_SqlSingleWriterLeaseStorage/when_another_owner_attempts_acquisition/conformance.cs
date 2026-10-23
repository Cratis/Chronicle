// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_another_owner_attempts_acquisition;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    SingleWriterLeaseAttempt _result;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    async Task Because()
    {
        _result = await Storage.TryAcquire(Claim with { Owner = new("owner-a") }, CancellationToken.None);
    }

    [Fact] void should_refuse_the_other_owner() => _result.Acquired.ShouldBeFalse();
    [Fact] void should_return_the_current_holder() => _result.Holder!.Owner.ShouldEqual(Claim.Owner);
}
