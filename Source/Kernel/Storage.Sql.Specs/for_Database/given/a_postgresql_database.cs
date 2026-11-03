// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_postgresql_database(PostgreSqlFixture fixture) : Specification
{
    protected PostgreSqlFixture Fixture => fixture;

    internal string ConnectionString;
    internal Database Database;
    internal ServiceProvider Services;
    internal readonly Cratis.Orleans.Storage.IJobsStorage JobsStorage = Substitute.For<Cratis.Orleans.Storage.IJobsStorage>();
    internal readonly List<string> AdditionalDatabases = [];
    internal string Store = "a-store";
    internal string Namespace = "a-namespace";

    async Task Establish()
    {
        ConnectionString = await fixture.CreateDatabase();
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance);
        Services = new ServiceCollection().AddSingleton<IReadModelMigrator>(migrator).AddSingleton(JobsStorage).BuildServiceProvider();
        Database = CreateDatabase();
    }

    internal Database CreateDatabase() => CreateDatabase(new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString }, Services);

    internal Database CreateDatabase(Configuration.Storage storage, IServiceProvider services) => new(
        services,
        Options.Create(new ChronicleOptions { Storage = storage }),
        new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
        new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
        services.GetRequiredService<IReadModelMigrator>());

    internal string LogicalStoreName => new NpgsqlConnectionStringBuilder(ConnectionString).Database + "+es+" + Store;

    internal async Task SeedStore(string name)
    {
        await using var cluster = await Database.Cluster();
        cluster.DbContext.EventStores.Add(new EventStore { Name = name });
        await cluster.DbContext.SaveChangesAsync();
    }

    internal async Task CreateLegacyDatabase(string name)
    {
        AdditionalDatabases.Add(name);
        await Execute(ConnectionString, $"CREATE DATABASE \"{name}\"");
    }

    internal async Task<string> Binding(DatabaseKind kind, string store, string ns)
    {
        await using var cluster = await Database.Cluster();
        return await cluster.DbContext.DatabaseNames.Where(row => row.Kind == kind && row.EventStore == store && row.Namespace == ns).Select(row => row.PhysicalName).SingleAsync();
    }

    internal string ConnectionFor(string name) => new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;

    internal static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Only specification-owned identifiers and constant fixture SQL are accepted.
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    internal readonly ProjectedColumn[] OccurrenceColumns =
    [
        new("Id", typeof(string), true, false, false, false),
        new("Value", typeof(int?), false, false, false, true),
        new(WellKnownProperties.ReadModelInstanceInitialized, typeof(bool), false, false, false, false)
    ];

    internal async Task<string[]> PhysicalDatabases()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE NOT datistemplate ORDER BY datname", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return [.. names];
    }

    internal async Task<int> BindingCount()
    {
        await using var cluster = await Database.Cluster();
        return await cluster.DbContext.DatabaseNames.CountAsync(row => row.Kind != DatabaseKind.Cluster);
    }

    internal async Task ProvisionPrimaryTable()
    {
        await using var scope = await Database.ReadModelTable(Store, Namespace, "primary", OccurrenceColumns);
    }

    internal async Task<string> ReadModelConnection() => ConnectionFor(await Binding(DatabaseKind.ReadModels, Store, Namespace));

    internal async Task CreateOlderTable(string connectionString, string table)
    {
        await Execute(connectionString, $"CREATE TABLE \"{table}\" (\"Id\" text NOT NULL PRIMARY KEY, \"{WellKnownProperties.ReadModelInstanceInitialized}\" boolean NOT NULL DEFAULT true)");
        await Execute(connectionString, $"INSERT INTO \"{table}\" (\"Id\") VALUES (\x27one\x27)");
    }

    /// <summary>
    /// Creates the occurrence table an older kernel left, which has a row but not the value column the read model has now.
    /// </summary>
    /// <param name="connectionString">The read model database.</param>
    /// <param name="table">The table.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task CreateOlderOccurrenceTable(string connectionString, string table)
    {
        await Execute(connectionString, $"CREATE TABLE \"{table}\" (\"id\" text NOT NULL PRIMARY KEY, \"{WellKnownProperties.LastHandledEventSequenceNumber}\" bigint NULL, \"{WellKnownProperties.ReadModelInstanceInitialized}\" boolean NOT NULL DEFAULT true)");
        await Execute(connectionString, $"INSERT INTO \"{table}\" (\"id\") VALUES (\x27one\x27)");
    }

    /// <summary>
    /// Gets every column and index of the current schema of a database.
    /// </summary>
    /// <param name="connectionString">The database to look in.</param>
    /// <returns>The sorted columns and indexes.</returns>
    internal static async Task<string> Catalog(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE((SELECT string_agg(table_name || \x27.\x27 || column_name, \x27,\x27 ORDER BY table_name, column_name) FROM information_schema.columns WHERE table_schema = current_schema()), \x27\x27) || \x27|\x27 || COALESCE((SELECT string_agg(indexname, \x27,\x27 ORDER BY indexname) FROM pg_indexes WHERE schemaname = current_schema()), \x27\x27)",
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Gets the columns and indexes of a table, or an empty string when there is no such table.
    /// </summary>
    /// <param name="connectionString">The database to look in.</param>
    /// <param name="table">The table.</param>
    /// <returns>The sorted columns and indexes.</returns>
    internal static async Task<string> TableShape(string connectionString, string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE((SELECT string_agg(column_name, \x27,\x27 ORDER BY column_name) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @t), \x27\x27) || \x27|\x27 || COALESCE((SELECT string_agg(indexname, \x27,\x27 ORDER BY indexname) FROM pg_indexes WHERE schemaname = current_schema() AND tablename = @t), \x27\x27)",
            connection);
        command.Parameters.AddWithValue("t", table);
        var shape = (string)(await command.ExecuteScalarAsync())!;
        return shape == "|" ? string.Empty : shape;
    }

    async Task Destroy()
    {
        if (ConnectionString is not null)
        {
            await using (var cluster = await Database.Cluster())
            {
                AdditionalDatabases.AddRange(await cluster.DbContext.DatabaseNames.Where(row => row.Kind != DatabaseKind.Cluster).Select(row => row.PhysicalName).ToArrayAsync());
            }

            foreach (var name in AdditionalDatabases.Distinct(StringComparer.Ordinal))
            {
                await fixture.DropDatabase(ConnectionFor(name));
            }

            await fixture.DropDatabase(ConnectionString);
        }

        await Services.DisposeAsync();
    }
}
