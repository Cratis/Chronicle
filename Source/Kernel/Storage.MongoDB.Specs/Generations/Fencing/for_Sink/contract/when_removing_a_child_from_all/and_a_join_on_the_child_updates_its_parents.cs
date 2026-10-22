// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child_from_all;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_removing_a_child_from_all;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_join_on_the_child_updates_its_parents(ReplicaSetMongoDBFixture fixture) : Contract.and_a_join_on_the_child_updates_its_parents<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
