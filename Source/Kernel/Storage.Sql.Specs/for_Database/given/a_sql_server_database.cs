// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_sql_server_database(SqlServerFixture fixture) : Specification
{
    const int DatabaseDoesNotExist = 3701;
    readonly List<ServiceProvider> _resolutionServices = [];

    internal string ConnectionString;
    internal Database Database;
    internal ServiceProvider Services;
    internal string Store = "a-store";
    internal string Namespace = "a-namespace";

    protected SqlServerFixture Fixture => fixture;

    async Task Establish()
    {
        ConnectionString = await fixture.CreateDatabase();
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance);
        Services = new ServiceCollection().AddSingleton<IReadModelMigrator>(migrator).BuildServiceProvider();
        Database = new(
            Services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString } }),
            new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
            new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
            migrator);
    }

    internal readonly RecordingConnectionResolver Resolver = new();

    internal Database ResolvingDatabase()
    {
        var resolution = new ConnectionResolutionOptions();
        resolution.Identity.UseCustom<RecordingConnectionResolver>();
        var services = new ServiceCollection()
            .AddSingleton(Services.GetRequiredService<IReadModelMigrator>())
            .AddSingleton(Resolver)
            .BuildServiceProvider();
        _resolutionServices.Add(services);
        var migrator = Services.GetRequiredService<IReadModelMigrator>();
        return new(
            services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString, ConnectionResolution = resolution } }),
            new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance),
            new UniqueConstraintMigrator(new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance), new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance), NullLogger<UniqueConstraintMigrator>.Instance),
            migrator);
    }

    protected Database CreateDatabase() => new(
        Services,
        Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString } }),
        Substitute.For<IEventSequenceMigrator>(),
        Substitute.For<IUniqueConstraintMigrator>(),
        Substitute.For<IReadModelMigrator>());

    internal readonly ProjectedColumn[] OccurrenceColumns =
    [
        new("Id", typeof(string), true, false, false, false),
        new("Value", typeof(int?), false, false, false, true),
        new(WellKnownProperties.ReadModelInstanceInitialized, typeof(bool), false, false, false, false)
    ];

    internal async Task<string[]> PhysicalDatabases()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT name FROM sys.databases ORDER BY name", connection);
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

    internal async Task<string> ReadModelDatabaseName()
    {
        await using var cluster = await Database.Cluster();
        return await cluster.DbContext.DatabaseNames
            .Where(row => row.Kind == DatabaseKind.ReadModels && row.EventStore == Store && row.Namespace == Namespace)
            .Select(row => row.PhysicalName)
            .SingleAsync();
    }

    internal string ConnectionFor(string name) => new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString;

    internal async Task CreateOlderTable(string connectionString, string table)
    {
        await Execute(connectionString, $"CREATE TABLE [{table}] ([Id] nvarchar(450) NOT NULL PRIMARY KEY, [{WellKnownProperties.ReadModelInstanceInitialized}] bit NOT NULL DEFAULT 1)");
        await Execute(connectionString, $"INSERT INTO [{table}] ([Id]) VALUES (\x27one\x27)");
    }

    internal static async Task CreateOlderOccurrenceTable(string connectionString, string table)
    {
        await Execute(connectionString, $"CREATE TABLE [{table}] ([id] nvarchar(450) NOT NULL PRIMARY KEY, [{WellKnownProperties.LastHandledEventSequenceNumber}] bigint NULL, [{WellKnownProperties.ReadModelInstanceInitialized}] bit NOT NULL DEFAULT 1)");
        await Execute(connectionString, $"INSERT INTO [{table}] ([id]) VALUES (\x27one\x27)");
    }

    internal static async Task<string> Catalog(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COALESCE((SELECT STRING_AGG(TABLE_NAME + \x27.\x27 + COLUMN_NAME, \x27,\x27) WITHIN GROUP (ORDER BY TABLE_NAME, COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME()), \x27\x27) + \x27|\x27 + COALESCE((SELECT STRING_AGG(i.name, \x27,\x27) WITHIN GROUP (ORDER BY i.name) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE t.schema_id = SCHEMA_ID() AND i.name IS NOT NULL), \x27\x27)",
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    internal static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Only specification-owned identifiers and constant fixture SQL are accepted.
        await using var command = new SqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the columns and indexes of a table, or an empty string when there is no such table.
    /// </summary>
    /// <param name="connectionString">The database to look in.</param>
    /// <param name="table">The table.</param>
    /// <returns>The sorted columns and indexes.</returns>
    internal static async Task<string> TableShape(string connectionString, string table)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COALESCE((SELECT STRING_AGG(COLUMN_NAME, \x27,\x27) WITHIN GROUP (ORDER BY COLUMN_NAME) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = @t), \x27\x27) + \x27|\x27 + COALESCE((SELECT STRING_AGG(i.name, \x27,\x27) WITHIN GROUP (ORDER BY i.name) FROM sys.indexes i WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + \x27.\x27 + QUOTENAME(@t)) AND i.name IS NOT NULL), \x27\x27)",
            connection);
        command.Parameters.AddWithValue("@t", table);
        var shape = (string)(await command.ExecuteScalarAsync())!;
        return shape == "|" ? string.Empty : shape;
    }

    async Task Destroy()
    {
        if (ConnectionString is not null)
        {
            await using (var cluster = await Database.Cluster())
            {
                var databases = await cluster.DbContext.DatabaseNames.Where(row => row.Kind != DatabaseKind.Cluster).Select(row => row.PhysicalName).ToArrayAsync();
                foreach (var name in databases)
                {
                    try
                    {
                        await fixture.DropDatabase(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString);
                    }
                    catch (SqlException ex) when (ex.Number == DatabaseDoesNotExist)
                    {
                        // A binding whose database a read never created, which is what the reads under specification must leave.
                    }
                }
            }

            await fixture.DropDatabase(ConnectionString);
        }

        await Services.DisposeAsync();
        foreach (var services in _resolutionServices)
        {
            await services.DisposeAsync();
        }
    }
}
