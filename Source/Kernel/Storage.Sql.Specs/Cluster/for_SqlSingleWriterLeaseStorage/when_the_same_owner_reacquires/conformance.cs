// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_the_same_owner_reacquires;

public abstract class conformance(LeaseHarness harness) : a_lease(harness)
{
    SingleWriterLeaseAttempt _result;
    DateTimeOffset _acquiredAt;

    async Task Establish()
    {
        _acquiredAt = (await Storage.TryAcquire(Claim, CancellationToken.None)).Holder!.AcquiredAt;
    }

    async Task Because()
    {
        _result = await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    [Fact] void should_preserve_the_acquisition_time() => _result.Holder!.AcquiredAt.ShouldEqual(_acquiredAt);
    [Fact] void should_acquire_again() => _result.Acquired.ShouldBeTrue();
}
