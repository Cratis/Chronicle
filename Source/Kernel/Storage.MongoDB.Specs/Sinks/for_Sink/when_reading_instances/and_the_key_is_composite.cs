// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_reading_instances;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_reading_instances;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_key_is_composite(ReplicaSetMongoDBFixture fixture) : Contract.and_the_key_is_composite<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { ConnectionString = fixture.ConnectionString };
}
