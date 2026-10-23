// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_preparing_an_initial_run;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_preparing_an_initial_run;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_read_model_already_has_state(ReplicaSetMongoDBFixture fixture) : Contract.and_the_read_model_already_has_state<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
