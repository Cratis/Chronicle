// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_another_cluster_and_service_attempt_acquisition(MongoDBFixture fixture) : given.a_lease(fixture)
{
    SingleWriterLeaseAttempt _result;

    async Task Establish()
    {
        await Storage.TryAcquire(Claim, CancellationToken.None);
    }

    async Task Because()
    {
        _result = await Storage.TryAcquire(Claim with { Owner = new("owner-b"), ClusterId = "other-cluster", ServiceId = "other-service" }, CancellationToken.None);
    }

    [Fact] void should_protect_the_whole_kernel_storage() => _result.Acquired.ShouldBeFalse();
}
