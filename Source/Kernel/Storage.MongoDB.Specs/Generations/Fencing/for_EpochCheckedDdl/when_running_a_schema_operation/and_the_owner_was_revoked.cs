// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_EpochCheckedDdl.when_running_a_schema_operation;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_owner_was_revoked(ReplicaSetMongoDBFixture fixture) : given.a_fenced_writer_against_a_replica_set(fixture)
{
    bool _ran;
    Exception? _error;

    async Task Because()
    {
        await Revoke();
        _error = await Catch.Exception(() => new EpochCheckedDdl(_gates).Run(_token, () =>
        {
            _ran = true;
            return Task.CompletedTask;
        }));
    }

    [Fact] void should_not_run_the_operation() => _ran.ShouldBeFalse();
    [Fact] void should_refuse_the_owner() => _error.ShouldBeOfExactType<FenceRevoked>();
}
