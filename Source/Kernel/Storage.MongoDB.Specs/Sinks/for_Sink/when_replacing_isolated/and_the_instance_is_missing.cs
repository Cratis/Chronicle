// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_replacing_isolated;

[Collection(MongoDBCollection.Name)]
public class and_the_instance_is_missing(MongoDBFixture fixture) : Contract.and_the_instance_is_missing<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
