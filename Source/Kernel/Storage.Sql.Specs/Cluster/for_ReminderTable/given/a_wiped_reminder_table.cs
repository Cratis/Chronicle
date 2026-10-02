// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.given;

public class a_wiped_reminder_table : a_reminder_table
{
    protected GrainId _grainId = GrainId.Create("observer", "system-reactor");
    protected const string ReminderName = "chronicle-observer:alerts";

    async Task Establish()
    {
        await _table.UpsertRow(CreateEntry(_grainId, ReminderName));

        // Reproduce the SQLite development reset: drop the schema and invalidate migration caches,
        // keeping the same database and reminder table instances alive for re-bootstrap.
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        _database.ClearTableMigrationCache(string.Empty);
    }
}
