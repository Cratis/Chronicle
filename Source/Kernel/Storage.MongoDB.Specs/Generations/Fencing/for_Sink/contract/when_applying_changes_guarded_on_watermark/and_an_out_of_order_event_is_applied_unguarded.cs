// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.contract.when_applying_changes_guarded_on_watermark;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_an_out_of_order_event_is_applied_unguarded(ReplicaSetMongoDBFixture fixture) : Contract.and_an_out_of_order_event_is_applied_unguarded<FencedMongoSinkHarness>
{
    protected override FencedMongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
