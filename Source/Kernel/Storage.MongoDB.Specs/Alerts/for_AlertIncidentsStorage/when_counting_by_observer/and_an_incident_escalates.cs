// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_counting_by_observer;

[Collection(MongoDBCollection.Name)]
public class and_an_incident_escalates(MongoDBFixture fixture) : Contract.and_an_incident_escalates<MongoAlertIncidentsHarness>
{
    protected override MongoAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
