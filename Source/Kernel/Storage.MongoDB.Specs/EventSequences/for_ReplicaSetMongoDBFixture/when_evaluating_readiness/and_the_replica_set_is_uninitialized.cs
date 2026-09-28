// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_evaluating_readiness;

public class and_the_replica_set_is_uninitialized : Specification
{
    BsonDocument _hello;
    bool _isReady;

    void Establish() => _hello = new BsonDocument { { "setName", "rs0" } };
    void Because() => _isReady = ReplicaSetMongoDBFixture.IsReady(_hello);

    [Fact] void should_reject_the_replica_set() => _isReady.ShouldBeFalse();
}
