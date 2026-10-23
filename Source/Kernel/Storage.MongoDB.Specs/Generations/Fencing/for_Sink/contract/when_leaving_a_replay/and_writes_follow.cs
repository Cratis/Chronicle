// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_leaving_a_replay;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_leaving_a_replay;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_writes_follow(ReplicaSetMongoDBFixture fixture) : Contract.and_writes_follow<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
