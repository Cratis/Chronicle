// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_container;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_removing_a_container;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_it_holds_the_read_model(ReplicaSetMongoDBFixture fixture) : Contract.and_it_holds_the_read_model<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
