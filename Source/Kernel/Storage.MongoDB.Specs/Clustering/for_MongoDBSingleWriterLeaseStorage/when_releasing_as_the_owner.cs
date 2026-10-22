// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_releasing_as_the_owner(MongoDBFixture fixture) : given.a_lease(fixture)
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
