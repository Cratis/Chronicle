// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts.for_SqlServerAlertIncidentsStorage.when_applying;

[Collection(SqlServerCollection.Name)]
public class and_the_sequence_exceeds_signed_storage(SqlServerFixture fixture) : Contract.and_the_sequence_exceeds_signed_storage<SqlServerAlertIncidentsHarness>
{
    protected override SqlServerAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
