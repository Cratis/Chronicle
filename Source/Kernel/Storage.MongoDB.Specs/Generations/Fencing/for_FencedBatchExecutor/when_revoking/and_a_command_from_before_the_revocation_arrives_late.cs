// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_command_from_before_the_revocation_arrives_late(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    FenceToken _newOwner;
    Exception? _error;

    async Task Because()
    {
        await CommitBeforeRevocation("before");
        _newOwner = await _gates.Claim(MaterializationKey, Generation);
        _error = await StaleBatch("late");
        await WriteBatch("new-owner", _newOwner);
        await Judge();
    }

    [Fact] void should_refuse_the_late_command() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_let_the_new_owner_write() => History.CountDocuments(FilterDefinition<BsonDocument>.Empty).ShouldEqual(DocumentsPerBatch * 2L);
    [Fact] void should_leave_a_consistent_history() => _violations.ShouldBeEmpty();
}
