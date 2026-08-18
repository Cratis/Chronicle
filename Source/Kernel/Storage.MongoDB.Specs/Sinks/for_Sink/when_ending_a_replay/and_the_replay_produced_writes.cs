// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_the_replay_produced_writes(MongoDBFixture fixture) : Contract.and_the_replay_produced_writes<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
