// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_replacing_isolated;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_stored_watermark_is_newer(ReplicaSetMongoDBFixture fixture) : Contract.a_conditional_replacement<FencedMongoSinkHarness>(30, false, IsolatedReplaceResult.Superseded, 10)
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
