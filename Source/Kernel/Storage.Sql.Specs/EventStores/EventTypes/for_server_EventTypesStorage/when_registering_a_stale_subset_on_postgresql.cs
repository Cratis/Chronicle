// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_server_EventTypesStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_registering_a_stale_subset_on_postgresql(PostgreSqlFixture fixture) : for_EventTypesStorage.when_registering_a_stale_migration_subset
{
    readonly given.ServerEventTypeDatabase _server = given.ServerEventTypeDatabase.PostgreSql(fixture);

    protected override async Task CreateDatabase() => _options = await _server.Create();
    protected override Task DropDatabase() => _server.Drop();
}
