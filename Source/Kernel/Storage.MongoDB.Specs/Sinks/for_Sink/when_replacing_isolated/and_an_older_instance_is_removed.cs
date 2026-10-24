// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_replacing_isolated;

[Collection(MongoDBCollection.Name)]
public class and_an_older_instance_is_removed(MongoDBFixture fixture) : Contract.a_conditional_replacement<MongoSinkHarness>(10, true, IsolatedReplaceResult.Removed, null)
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
