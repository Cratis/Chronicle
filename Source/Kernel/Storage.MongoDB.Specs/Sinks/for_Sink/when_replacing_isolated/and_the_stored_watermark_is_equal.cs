// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_replacing_isolated;

[Collection(MongoDBCollection.Name)]
public class and_the_stored_watermark_is_equal(MongoDBFixture fixture) : Contract.a_conditional_replacement<MongoSinkHarness>(20, false, IsolatedReplaceResult.Replaced, 20)
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
