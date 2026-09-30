// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_observing_instances;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_read_model_has_no_instances(ReplicaSetMongoDBFixture fixture) : Contract.and_the_read_model_has_no_instances<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
