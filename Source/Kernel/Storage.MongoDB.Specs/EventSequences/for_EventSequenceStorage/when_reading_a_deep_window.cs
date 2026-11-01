// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_a_deep_window(ReplicaSetMongoDBFixture fixture) : Cratis.Chronicle.Storage.EventSequences.for_IEventSequenceStorage.when_reading_a_deep_window<MongoEventSequenceWindowHarness>
{
    protected override MongoEventSequenceWindowHarness CreateHarness() => new() { Fixture = fixture };
}
