// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration.given;

/// <summary>
/// Builds the closed streams table in the layout before the exact spelling migration, and applies that migration on demand.
/// </summary>
/// <param name="connectionString">The connection string of the isolated database.</param>
/// <param name="sqlite">Whether the database is SQLite, otherwise the provider is taken from the connection string.</param>
public sealed class UpgradeHarness(string connectionString, bool sqlite = false)
{
    public async Task PrepareEarlierLayout(params string[] eventSourceIds)
    {
        await using var context = Context();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var generator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        foreach (var (id, type) in assembly.Migrations.Where(entry => entry.Key != "NS-ClosedStreams-v20_0_2"))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);

            // SQLite rebuilds a table to change its key, which needs the target model of the migration.
            foreach (var command in sqlite ? generator.Generate(migration.UpOperations, context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel)) : generator.Generate(migration.UpOperations))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }

        foreach (var eventSourceId in eventSourceIds)
        {
            await Insert(eventSourceId);
        }
    }

    public Task Insert(string eventSourceId, string extraColumns = "", string extraValues = "") =>
        Execute($"INSERT INTO \"ClosedStreams\" (\"EventSequenceId\", \"StreamType\", \"StreamId\", \"EventSourceId\", \"EventSourceType\", \"Owner\", \"Dimensions\", \"SequenceNumber\"{extraColumns}) VALUES ('event-log', 'type', 'id', '{eventSourceId}', '', 'owner', 12, 1{extraValues})");

    public async Task Apply(bool up)
    {
        await using var context = Context();
        var migration = new v20_0_2 { ActiveProvider = context.Database.ProviderName };
        foreach (var command in context.GetService<IMigrationsSqlGenerator>().Generate(up ? migration.UpOperations : migration.DownOperations))
        {
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
    }

    public async Task Migrate()
    {
        await using var context = Context();
        await context.Database.MigrateAsync();
    }

    public async Task PrepareWideKeys(int streamIdLength)
    {
        await PrepareEarlierLayout("source");
        await Execute($"UPDATE ClosedStreams SET EventSourceId = REPLICATE(N'a', 255), StreamId = REPLICATE(N'p', {streamIdLength})");
        await Execute("INSERT INTO ClosedStreams (EventSequenceId, StreamType, StreamId, EventSourceId, EventSourceType, Owner, Dimensions, SequenceNumber, ClosedAt) "
            + "SELECT EventSequenceId, StreamType, StreamId, REPLICATE(N'b', 255), EventSourceType, Owner, Dimensions, SequenceNumber, ClosedAt FROM ClosedStreams");
    }

    public async Task<List<int>> Numbers(string sql)
    {
        await using var context = Context();
        return await context.Database.SqlQueryRaw<int>(sql).ToListAsync();
    }

    public Task<List<int>> KeyBytes(bool upgraded = false) => Numbers(
        "SELECT DATALENGTH(EventSequenceId) + DATALENGTH(StreamType) + DATALENGTH(StreamId) "
        + "+ DATALENGTH(EventSourceId) + DATALENGTH(EventSourceType) + DATALENGTH(Owner) + DATALENGTH(Dimensions)"
        + (upgraded ? " + DATALENGTH(ExactKey)" : "") + " AS Value FROM ClosedStreams");

    public Task<List<string>> Rows() => Strings(
        "SELECT CONCAT(EventSequenceId, '/', StreamType, '/', StreamId, '/', EventSourceId, '/', EventSourceType, "
        + "'/', Owner, '/', Dimensions, '/', SequenceNumber, '/', ClosedAt) AS Value FROM ClosedStreams ORDER BY 1");

    public Task<List<string>> History() => Strings("SELECT MigrationId AS Value FROM __EFMigrationsHistory ORDER BY MigrationId");

    public Task<List<string>> PrimaryKeyColumns() => Strings(
        "SELECT col.name AS Value FROM sys.indexes idx "
        + "JOIN sys.index_columns ic ON ic.object_id = idx.object_id AND ic.index_id = idx.index_id "
        + "JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id "
        + "WHERE idx.object_id = OBJECT_ID(N'ClosedStreams') AND idx.is_primary_key = 1 ORDER BY ic.key_ordinal");

    public async Task Execute(string sql)
    {
        await using var context = Context();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    public async Task<List<string>> Strings(string sql)
    {
        await using var context = Context();
        return await context.Database.SqlQueryRaw<string>(sql).ToListAsync();
    }

    public Task<List<string>> Spellings() => Strings("SELECT \"StreamType\" || '/' || \"StreamId\" || '/' || \"EventSourceId\" AS \"Value\" FROM \"ClosedStreams\" ORDER BY 1");

    public async Task<List<string>> Flags() =>
        [.. (await Strings("SELECT CAST(\"ExactSpelling\" AS varchar(5)) AS \"Value\" FROM \"ClosedStreams\"")).Select(flag => string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase) || flag == "1" ? "1" : "0")];

    NamespaceDbContext Context() => new(sqlite
        ? new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options
        : SpecDbContextOptions.ForConnection<NamespaceDbContext>(connectionString));
}
