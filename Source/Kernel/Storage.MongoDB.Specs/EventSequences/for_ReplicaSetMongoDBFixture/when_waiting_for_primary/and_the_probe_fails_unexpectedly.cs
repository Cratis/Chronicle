// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_waiting_for_primary;

public class and_the_probe_fails_unexpectedly : Specification
{
    int _attempts;
    Exception _error;
    ReplicaSetMongoDBFixture _fixture;

    void Establish() => _fixture = new();
    async Task Because() => _error = await Catch.Exception(() => _fixture.WaitForPrimary(Probe));

    Task<BsonDocument> Probe(CancellationToken _)
    {
        _attempts++;
        return Task.FromException<BsonDocument>(new InvalidOperationException("Unexpected probe failure"));
    }

    [Fact]
    void should_propagate_without_retrying()
    {
        _error.ShouldBeOfExactType<InvalidOperationException>();
        _attempts.ShouldEqual(1);
    }
}
