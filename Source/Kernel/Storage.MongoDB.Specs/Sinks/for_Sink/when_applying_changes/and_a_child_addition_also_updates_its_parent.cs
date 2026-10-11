// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_a_child_addition_also_updates_its_parent(MongoDBFixture fixture) : Contract.and_a_child_addition_also_updates_its_parent<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
