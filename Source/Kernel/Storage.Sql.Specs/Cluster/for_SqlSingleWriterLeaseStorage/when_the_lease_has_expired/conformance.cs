// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_the_lease_has_expired;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    SingleWriterLeaseAttempt _result;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
        await Harness.Expire();
    }

    async Task Because()
    {
        _result = await Storage.TryAcquire(Claim with { Owner = new("owner-b") }, CancellationToken.None);
    }

    [Fact] void should_admit_another_owner() => _result.Acquired.ShouldBeTrue();
    [Fact] void should_record_the_new_holder() => _result.Holder!.Owner.Value.ShouldEqual("owner-b");
}
