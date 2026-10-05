// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_PostgreSqlAlertIncidentsStorage.when_applying;

[Collection(PostgreSqlCollection.Name)]
public class and_escalation_changes_its_target(PostgreSqlFixture fixture) : Contract.and_escalation_changes_its_target<PostgreSqlAlertIncidentsHarness>
{
    protected override PostgreSqlAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
