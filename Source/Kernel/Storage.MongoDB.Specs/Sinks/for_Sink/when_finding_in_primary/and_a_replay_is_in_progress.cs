// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_in_primary;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

[Collection(MongoDBCollection.Name)]
public class and_a_replay_is_in_progress(MongoDBFixture fixture) : Contract.and_a_replay_is_in_progress<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
