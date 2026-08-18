// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes_guarded_on_watermark;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

[Collection(MongoDBCollection.Name)]
public class and_the_read_model_does_not_exist_yet(MongoDBFixture fixture) : Contract.and_the_read_model_does_not_exist_yet<MongoSinkHarness>
{
    protected override MongoSinkHarness CreateHarness() => new() { Fixture = fixture };
}
