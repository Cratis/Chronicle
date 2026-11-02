// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage.given;

public class a_postgresql_history_storage(PostgreSqlFixture fixture) : a_postgresql_database(fixture)
{
    protected ProjectionDefinitionHistoryStorage _storage;
    protected ProjectionDefinitionHistoryEntry _entry;
    protected ProjectionDefinitionHistoryEntry[] _entries;

    async Task Establish()
    {
        _storage = new(Store, Database);
        _entry = history_entry.Create();
        await using var scope = await Database.EventStore(Store);
    }

    protected async Task Read() => _entries = (await _storage.GetFor(_entry.Projection)).ToArray();
}
