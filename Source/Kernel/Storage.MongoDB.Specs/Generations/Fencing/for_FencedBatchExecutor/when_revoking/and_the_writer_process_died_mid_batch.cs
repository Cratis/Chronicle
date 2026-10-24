// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_writer_process_died_mid_batch(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    MongoClient _deadProcess;
    long _epochAfterRevocation;

    async Task Because()
    {
        await CommitBeforeRevocation("before");
        await SetTransactionLifetime(2);
        try
        {
            // The dead writer admitted its batch and wrote part of it, then vanished without committing or ending its session.
            _deadProcess = new MongoClient(ConnectionString);
            var deadDatabase = _deadProcess.GetDatabase(_database.DatabaseNamespace.DatabaseName);
            var session = await _deadProcess.StartSessionAsync();
            session.StartTransaction(MongoFenceStore.TransactionOptions);
            var transaction = new MongoFenceTransaction(deadDatabase, session);
            (await transaction.Admit(_token, FenceBatchId.New(), CancellationToken.None)).ShouldBeTrue();
            await deadDatabase.GetCollection<BsonDocument>("history").InsertOneAsync(session, FenceHistoryOracle.Document("dead", 0, _token.Epoch));
            _revoked.Add("dead");

            _epochAfterRevocation = await Revoke();
        }
        finally
        {
            await SetTransactionLifetime(60);
        }

        await Judge();
    }

    void Destroy() => _deadProcess?.Dispose();

    [Fact] void should_complete_the_revocation() => _epochAfterRevocation.ShouldEqual(_token.Epoch + 1);
    [Fact] void should_leave_a_consistent_history() => _violations.ShouldBeEmpty();
}
