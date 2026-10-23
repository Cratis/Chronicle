// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_applying_changes_guarded_on_watermark;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_read_model_does_not_exist_yet(ReplicaSetMongoDBFixture fixture) : Contract.and_the_read_model_does_not_exist_yet<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
