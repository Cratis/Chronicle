// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_PostgreSqlAlertIncidentsStorage.when_querying;

[Collection(PostgreSqlCollection.Name)]
public class and_a_similar_store_name_holds_the_incident(PostgreSqlFixture fixture) : Contract.and_a_similar_store_name_holds_the_incident<PostgreSqlAlertIncidentsHarness>
{
    protected override PostgreSqlAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
