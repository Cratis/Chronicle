// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_releasing_with_another_owner(MongoDBFixture fixture) : given.a_lease(fixture)
{
    SingleWriterLeaseAttempt _result;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    async Task Because()
    {
        await Storage.Release(new("owner-a"), CancellationToken.None);
        _result = await Storage.TryAcquire(Claim with { Owner = new("owner-b") }, CancellationToken.None);
    }

    [Fact] void should_keep_the_original_lease() => _result.Acquired.ShouldBeFalse();
}
