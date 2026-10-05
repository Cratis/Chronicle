// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_applying;

[Collection(MongoDBCollection.Name)]
public class and_an_escalation_follows_a_clear(MongoDBFixture fixture) : Contract.and_an_escalation_follows_a_clear<MongoAlertIncidentsHarness>
{
    protected override MongoAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
