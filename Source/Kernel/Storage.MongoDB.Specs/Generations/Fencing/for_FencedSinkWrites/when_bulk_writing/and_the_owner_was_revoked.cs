// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites.when_bulk_writing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_owner_was_revoked(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _count;

    async Task Because()
    {
        await Revoke();
        _error = await Catch.Exception(() => _writes.BulkWrite(_models, [Insert(1), Insert(2)], CancellationToken.None));
        _count = await Count();
    }

    [Fact] void should_refuse_the_owner() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_not_write_anything() => _count.ShouldEqual(0L);
}
