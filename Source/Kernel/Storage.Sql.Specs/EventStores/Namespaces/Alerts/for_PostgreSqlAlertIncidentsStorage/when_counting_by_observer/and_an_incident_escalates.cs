// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_PostgreSqlAlertIncidentsStorage.when_counting_by_observer;

[Collection(PostgreSqlCollection.Name)]
public class and_an_incident_escalates(PostgreSqlFixture fixture) : Contract.and_an_incident_escalates<PostgreSqlAlertIncidentsHarness>
{
    protected override PostgreSqlAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
