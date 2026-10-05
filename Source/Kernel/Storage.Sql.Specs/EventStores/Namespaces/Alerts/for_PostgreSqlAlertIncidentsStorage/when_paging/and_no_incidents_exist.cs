// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_PostgreSqlAlertIncidentsStorage.when_paging;

[Collection(PostgreSqlCollection.Name)]
public class and_no_incidents_exist(PostgreSqlFixture fixture) : Contract.and_no_incidents_exist<PostgreSqlAlertIncidentsHarness>
{
    protected override PostgreSqlAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
