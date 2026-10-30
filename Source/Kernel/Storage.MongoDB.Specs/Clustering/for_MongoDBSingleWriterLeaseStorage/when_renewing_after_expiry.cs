// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_renewing_after_expiry(MongoDBFixture fixture) : given.a_lease(fixture)
{
    bool _renewed;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
        await Expire();
    }

    async Task Because()
    {
        _renewed = await Storage.Renew(Claim.Owner, Claim.Ttl, CancellationToken.None);
    }

    [Fact] void should_refuse_late_renewal() => _renewed.ShouldBeFalse();
}
