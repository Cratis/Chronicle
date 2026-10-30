// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage;

[Collection(MongoDBCollection.Name)]
public class when_two_owners_race(MongoDBFixture fixture) : given.a_lease(fixture)
{
    int _winners;

    async Task Because()
    {
        var results = await Task.WhenAll(
            Task.Run(() => Storage.TryAcquire(Claim, CancellationToken.None)),
            Task.Run(() => Storage.TryAcquire(Claim with { Owner = new("owner-b") }, CancellationToken.None)));
        _winners = results.Count(result => result.Acquired);
    }

    [Fact] void should_admit_exactly_one_owner() => _winners.ShouldEqual(1);
}
