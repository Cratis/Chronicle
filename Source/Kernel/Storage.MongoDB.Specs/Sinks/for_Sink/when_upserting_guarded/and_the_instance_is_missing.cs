// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_upserting_guarded;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_upserting_guarded;

[Collection(MongoDBCollection.Name)]
public class and_the_instance_is_missing(MongoDBFixture fixture) : Contract.a_guarded_upsert<MongoSinkHarness>(null, 20)
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
