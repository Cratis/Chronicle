// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_leaving_a_replay;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_leaving_a_replay;

[Collection(MongoDBCollection.Name)]
public class and_writes_follow(MongoDBFixture fixture) : Contract.and_writes_follow<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
