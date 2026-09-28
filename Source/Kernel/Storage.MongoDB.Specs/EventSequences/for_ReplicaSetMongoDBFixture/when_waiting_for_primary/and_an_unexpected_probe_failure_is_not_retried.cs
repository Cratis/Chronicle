// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_waiting_for_primary;

public class and_an_unexpected_probe_failure_is_not_retried : Specification
{
    int _attempts;
    ReplicaSetMongoDBFixture _fixture;

    void Establish() => _fixture = new();
    async Task Because() => await Catch.Exception(() => _fixture.WaitForPrimary(Probe));

    Task<BsonDocument> Probe(CancellationToken _)
    {
        _attempts++;
        return Task.FromException<BsonDocument>(new InvalidOperationException("Unexpected probe failure"));
    }

    [Fact] void should_stop_after_one_attempt() => _attempts.ShouldEqual(1);
}
