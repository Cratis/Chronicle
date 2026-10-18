// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_wiping;

public class and_another_sqlite_instance_has_ensured_a_table : given.a_sqlite_database
{
    Database _other;

    async Task Establish()
    {
        _other = CreateDatabase();
        await using var scope = await _other.EventSequenceTable("store", "namespace", "events");

        // The substituted table migrator does not open SQLite; create the target for the wipe to discover.
        await scope.DbContext.Database.OpenConnectionAsync();
    }

    async Task Because()
    {
        await _database.Wipe();
        await using var scope = await _other.EventSequenceTable("store", "namespace", "events");
    }

    [Fact] async Task should_ensure_the_table_again() =>
        await _eventSequenceMigrator.Received(2).EnsureTableMigrated("events", Arg.Any<EventSequenceDbContext>());
}
#endif
