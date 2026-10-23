// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_ending_a_replay;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_second_replay_follows_the_first(ReplicaSetMongoDBFixture fixture) : Contract.and_a_second_replay_follows_the_first<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
