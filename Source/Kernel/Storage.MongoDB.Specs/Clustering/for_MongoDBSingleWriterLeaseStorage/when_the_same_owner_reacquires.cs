// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_the_same_owner_reacquires(MongoDBFixture fixture) : given.a_lease(fixture)
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
