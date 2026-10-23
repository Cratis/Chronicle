// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_the_lease_is_absent(MongoDBFixture fixture) : given.a_lease(fixture)
{
    SingleWriterLeaseAttempt _result;

    async Task Because()
    {
        _result = await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    [Fact] void should_acquire_the_lease() => _result.Acquired.ShouldBeTrue();
    [Fact] void should_return_the_owner() => _result.Holder!.Owner.ShouldEqual(Claim.Owner);
    [Fact] void should_return_the_host() => _result.Holder!.HostName.ShouldEqual(Claim.HostName);
    [Fact] void should_return_the_version() => _result.Holder!.Version.ShouldEqual(Claim.Version);
    [Fact] void should_use_the_requested_lifetime() => (_result.Holder!.ExpiresAt - _result.Holder.AcquiredAt).ShouldBeInRange(TimeSpan.FromSeconds(29), TimeSpan.FromSeconds(31));
}
