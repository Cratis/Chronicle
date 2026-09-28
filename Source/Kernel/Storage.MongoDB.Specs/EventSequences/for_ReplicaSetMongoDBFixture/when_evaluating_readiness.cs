// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture;

public class when_evaluating_readiness : Specification
{
    [Fact]
    void should_accept_a_writable_primary_in_the_expected_replica_set() =>
        ReplicaSetMongoDBFixture.IsReady(new BsonDocument { { "setName", "rs0" }, { "isWritablePrimary", true } }).ShouldBeTrue();
    [Fact]
    void should_reject_a_secondary() =>
        ReplicaSetMongoDBFixture.IsReady(new BsonDocument { { "setName", "rs0" }, { "isWritablePrimary", false } }).ShouldBeFalse();
    [Fact]
    void should_reject_an_uninitialized_replica_set() =>
        ReplicaSetMongoDBFixture.IsReady(new BsonDocument { { "setName", "rs0" } }).ShouldBeFalse();
    [Fact]
    void should_reject_a_standalone_server() =>
        ReplicaSetMongoDBFixture.IsReady(new BsonDocument { { "isWritablePrimary", true } }).ShouldBeFalse();
    [Fact]
    void should_reject_a_different_replica_set() =>
        ReplicaSetMongoDBFixture.IsReady(new BsonDocument { { "setName", "other" }, { "isWritablePrimary", true } }).ShouldBeFalse();
    [Fact]
    async Task should_retry_a_server_selection_timeout_before_accepting_the_primary()
    {
        var attempts = 0;
        var fixture = new ReplicaSetMongoDBFixture();

        Task<BsonDocument> Probe(CancellationToken _)
        {
            attempts++;
            return attempts == 1
                ? Task.FromException<BsonDocument>(new TimeoutException("Server selection timed out"))
                : Task.FromResult(new BsonDocument { { "setName", "rs0" }, { "isWritablePrimary", true } });
        }

        await fixture.WaitForPrimary(Probe).WaitAsync(TimeSpan.FromSeconds(3));
        attempts.ShouldEqual(2);
    }
}
