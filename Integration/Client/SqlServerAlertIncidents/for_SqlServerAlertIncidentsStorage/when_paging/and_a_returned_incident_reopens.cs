// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts.for_SqlServerAlertIncidentsStorage.when_paging;

[Collection(SqlServerCollection.Name)]
public class and_a_returned_incident_reopens(SqlServerFixture fixture) : Contract.and_a_returned_incident_reopens<SqlServerAlertIncidentsHarness>
{
    protected override SqlServerAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
