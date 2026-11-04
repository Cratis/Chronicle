// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_server_EventTypesStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_changing_metadata_on_postgresql(PostgreSqlFixture fixture) : for_EventTypesStorage.when_changing_only_the_visibility_through_a_batch
{
    readonly given.ServerEventTypeDatabase _server = given.ServerEventTypeDatabase.PostgreSql(fixture);

    protected override async Task CreateDatabase() => _options = await _server.Create();
    protected override Task DropDatabase() => _server.Drop();
}
