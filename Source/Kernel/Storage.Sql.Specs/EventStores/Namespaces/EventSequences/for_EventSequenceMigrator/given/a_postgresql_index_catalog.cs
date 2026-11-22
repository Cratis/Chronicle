// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

public class a_postgresql_index_catalog(PostgreSqlFixture fixture) : Specification
{
    /// <summary>
    /// Fits the table limit, but forces byte truncation of the legacy index name and includes quoting.
    /// </summary>
    protected const string Table = "events_quoted\"_éééééééééééééééééé";
    protected string _connectionString;
    protected TableMigrator<EventSequenceDbContext> _tables;
    protected EventSequenceMigrator _migrator;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _tables = new(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>());
        _migrator = new(_tables, Substitute.For<ILogger<EventSequenceMigrator>>());
        await using var context = Context();
        await context.EnsureTableExists();
    }

    protected EventSequenceDbContext Context() => new(SpecDbContextOptions.Kernel<EventSequenceDbContext>(_connectionString), Table, _migrator);

    protected static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";

    protected async Task Execute(string sql)
    {
        await using var context = Context();
#pragma warning disable EF1002 // Only locally constructed, quoted fixture identifiers and constant test definitions.
        await context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }

    protected async Task<string[]> Read(string sql)
    {
        await using var context = Context();
        return await context.Database.SqlQueryRaw<string>(sql, new NpgsqlParameter("table", Table), new NpgsqlParameter("index", $"IX_{Table}_SequenceNumber")).ToArrayAsync();
    }

    protected Task<string[]> Inventory() => Read("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = current_schema() AND tablename = @table ORDER BY indexname");

    protected Task<string[]> LegacyIndex() => Read("SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = current_schema() AND tablename = @table AND indexname = CAST(@index AS name)");

    protected Task AddLegacyIndex(string definition = "(\"SequenceNumber\")") => Execute($"CREATE INDEX {Quote($"IX_{Table}_SequenceNumber")} ON {Quote(Table)} {definition}");

    protected async Task Restart()
    {
        _tables.ClearMigrationCacheForConnectionString(_connectionString);
        await using var context = Context();
        await context.EnsureTableExists();
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);
}
