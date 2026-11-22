// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(SqlServerCollection.Name)]
public class when_preserving_sql_server_indexes(SqlServerFixture fixture)
{
    [Fact]
    async Task should_keep_the_full_index_inventory_across_restarts()
    {
        var connectionString = await fixture.CreateDatabase();
        try
        {
            var tables = new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>());
            var migrator = new EventSequenceMigrator(tables, Substitute.For<ILogger<EventSequenceMigrator>>());
            await using var context = new EventSequenceDbContext(SpecDbContextOptions.ForConnection<EventSequenceDbContext>(connectionString), "events", migrator);
            await context.EnsureTableExists();
            var before = await Inventory(context);
            before.Any(index => index.StartsWith("IX_events_SequenceNumber:", StringComparison.Ordinal)).ShouldBeTrue();
            for (var restart = 0; restart < 2; restart++)
            {
                tables.ClearMigrationCacheForConnectionString(connectionString);
                await context.EnsureTableExists();
                Assert.Equal(before, await Inventory(context));
            }
        }
        finally
        {
            await fixture.DropDatabase(connectionString);
        }
    }

    static Task<string[]> Inventory(EventSequenceDbContext context) => context.Database.SqlQueryRaw<string>(
        "SELECT CONCAT(i.name, ':', i.type_desc, ':', i.is_unique, ':', i.is_primary_key, ':'," +
        " ic.key_ordinal, ':', ic.is_descending_key, ':', ic.is_included_column, ':', c.name, ':', i.filter_definition) AS Value" +
        " FROM sys.indexes i" +
        " JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id" +
        " JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id" +
        " WHERE i.object_id = OBJECT_ID(@table) ORDER BY i.name, ic.index_column_id",
        new SqlParameter("table", "events")).ToArrayAsync();
}
