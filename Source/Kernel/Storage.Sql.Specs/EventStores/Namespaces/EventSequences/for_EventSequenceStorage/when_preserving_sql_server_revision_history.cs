// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

[Collection(SqlServerCollection.Name)]
public class when_preserving_sql_server_revision_history(SqlServerFixture fixture) : EventRevisionContract
{
    string? _connectionString;
    given.EventSequenceStorageHost? _host;

    async Task Destroy()
    {
        _host?.Dispose();
        if (_connectionString is not null)
        {
            await fixture.DropDatabase(_connectionString);
        }
    }

    protected override async Task<IEventSequenceStorage> OpenStorage()
    {
        _connectionString ??= await fixture.CreateDatabase();
        return (_host ??= given.EventSequenceStorageHost.ForSqlServer(_connectionString, Identities, EventTypes)).Open();
    }
}
