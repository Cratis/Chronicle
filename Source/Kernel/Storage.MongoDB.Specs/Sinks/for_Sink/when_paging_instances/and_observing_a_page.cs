// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_paging_instances;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_observing_a_page(ReplicaSetMongoDBFixture fixture) : Contract.and_observing_a_page<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
