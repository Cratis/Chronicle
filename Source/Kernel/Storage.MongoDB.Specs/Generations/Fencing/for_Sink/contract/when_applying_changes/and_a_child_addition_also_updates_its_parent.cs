// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_applying_changes;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_child_addition_also_updates_its_parent(ReplicaSetMongoDBFixture fixture) : Contract.and_a_child_addition_also_updates_its_parent<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
