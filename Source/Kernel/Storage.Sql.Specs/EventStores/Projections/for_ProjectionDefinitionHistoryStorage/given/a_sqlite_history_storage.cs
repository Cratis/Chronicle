// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage.given;

public class a_sqlite_history_storage : a_sqlite_database
{
    protected ProjectionDefinitionHistoryStorage _storage;
    protected ProjectionDefinitionHistoryEntry _entry;
    protected ProjectionDefinitionHistoryEntry[] _entries;

    async Task Establish()
    {
        _storage = new("history-store", _database);
        _entry = history_entry.Create();
        await using var scope = await _database.EventStore("history-store");
    }

    protected async Task Read() => _entries = (await _storage.GetFor(_entry.Projection)).ToArray();
}
