// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts.for_SqlServerAlertIncidentsStorage.when_querying;

[Collection(SqlServerCollection.Name)]
public class and_other_stores_precede_the_matching_store(SqlServerFixture fixture) : Contract.and_other_stores_precede_the_matching_store<SqlServerAlertIncidentsHarness>
{
    protected override SqlServerAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
