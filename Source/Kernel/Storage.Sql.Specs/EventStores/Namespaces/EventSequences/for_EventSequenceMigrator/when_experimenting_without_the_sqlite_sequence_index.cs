// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_experimenting_without_the_sqlite_sequence_index
{
    [Fact]
    async Task should_retain_the_production_inventory_and_keep_experiment_reads_equivalent()
    {
        using var host = EventSequenceStorageHost.ForSqlite(given.SequenceIndexReadCorpus.Identities(), Substitute.For<IEventTypesStorage>());
        await using (var context = host.CreateContext())
        {
            const string inventorySql = "SELECT name || ':' || COALESCE(sql, '') AS Value FROM sqlite_master WHERE type = 'index' ORDER BY name";
            var before = await context.Database.SqlQueryRaw<string>(inventorySql).ToArrayAsync();
            before.Any(index => index.Contains("_SequenceNumber:", StringComparison.Ordinal)).ShouldBeTrue();
            var tables = new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>());
            for (var restart = 0; restart < 2; restart++)
            {
                tables.ClearMigrationCacheForConnectionString(context.Database.GetConnectionString()!);
                await context.EnsureTableExists();
                Assert.Equal(before, await context.Database.SqlQueryRaw<string>(inventorySql).ToArrayAsync());
            }
        }
        await given.SequenceIndexReadCorpus.Verify(host);
    }
}
