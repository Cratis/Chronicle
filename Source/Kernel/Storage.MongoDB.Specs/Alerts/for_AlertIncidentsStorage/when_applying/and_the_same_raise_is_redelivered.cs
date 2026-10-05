// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_applying;

[Collection(MongoDBCollection.Name)]
public class and_the_same_raise_is_redelivered(MongoDBFixture fixture) : Contract.and_the_same_raise_is_redelivered<MongoAlertIncidentsHarness>
{
    protected override MongoAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
