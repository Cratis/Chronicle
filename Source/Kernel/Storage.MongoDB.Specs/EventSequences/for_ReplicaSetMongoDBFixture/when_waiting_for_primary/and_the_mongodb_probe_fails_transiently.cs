// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_waiting_for_primary;

public class and_the_mongodb_probe_fails_transiently : Specification
{
    int _attempts;
    ReplicaSetMongoDBFixture _fixture;

    void Establish() => _fixture = new();
    async Task Because() => await _fixture.WaitForPrimary(Probe);

    Task<BsonDocument> Probe(CancellationToken _)
    {
        _attempts++;
        return _attempts == 1
            ? Task.FromException<BsonDocument>(new MongoClientException("Connection not ready"))
            : Task.FromResult(new BsonDocument { { "setName", "rs0" }, { "isWritablePrimary", true } });
    }

    [Fact] void should_retry_and_accept_the_primary() => _attempts.ShouldEqual(2);
}
