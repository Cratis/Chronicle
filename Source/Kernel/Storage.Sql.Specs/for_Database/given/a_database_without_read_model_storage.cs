// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// The real <see cref="Database"/> over SQLite with the real read model migrator, where only the cluster file exists.
/// Anything that provisions leaves a file or a table behind, which the specifications observe.
/// </summary>
public class a_database_without_read_model_storage : a_sqlite_database
{
    protected readonly ProjectedColumn[] _columns = [new("Id", typeof(string), true, false, false, false), new("Value", typeof(int), false, false, false, false)];

    ServiceProvider? _resolutionServices;

    void Establish()
    {
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance),
            NullLogger<ReadModelMigrator>.Instance);
        _readModelMigrator.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.EnsureTableMigrated(call.Arg<string>(), call.Arg<ReadModelDbContext>()));
        _readModelMigrator.ProbeExistingTable(Arg.Any<string>(), Arg.Any<ReadModelDbContext>())
            .Returns(call => migrator.ProbeExistingTable(call.Arg<string>(), call.Arg<ReadModelDbContext>()));
    }

    /// <summary>
    /// Gets the resolver that connection resolution asks.
    /// </summary>
    protected RecordingConnectionResolver Resolver { get; } = new();

    /// <summary>
    /// Creates a database over the same storage, with connection resolution configured.
    /// </summary>
    /// <returns>The <see cref="Database"/>.</returns>
    protected Database ResolvingDatabase()
    {
        var resolution = new ConnectionResolutionOptions();
        resolution.Identity.UseCustom<RecordingConnectionResolver>();
        _resolutionServices = new ServiceCollection().AddSingleton(_readModelMigrator).AddSingleton(Resolver).BuildServiceProvider();
        return new(
            _resolutionServices,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _connectionString, ConnectionResolution = resolution } }),
            _eventSequenceMigrator,
            Substitute.For<IUniqueConstraintMigrator>(),
            _readModelMigrator);
    }

    /// <summary>
    /// Runs SQL against a database file, without pooling the connection.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <param name="sql">The SQL.</param>
    /// <returns>Awaitable task.</returns>
    protected static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Specification-owned SQL.
        await using var command = new SqliteCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets everything a database file defines: its tables, columns and indexes.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <returns>The definition of every object.</returns>
    protected static async Task<string> Catalog(string connectionString)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder(connectionString) { Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("SELECT COALESCE(group_concat(type || ':' || name || ':' || COALESCE(sql, ''), ';'), '') FROM (SELECT * FROM sqlite_master ORDER BY name)", connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Provisions a table, which creates the read model database, and gets how to connect to it.
    /// </summary>
    /// <returns>The connection string of the read model database.</returns>
    protected async Task<string> ProvisionReadModelDatabase()
    {
        await using var scope = await _database.ReadModelTable("store", "namespace", "primary", _columns);
        return scope.DbContext.Database.GetConnectionString()!;
    }

    async Task Destroy()
    {
        if (_resolutionServices is not null)
        {
            await _resolutionServices.DisposeAsync();
        }
    }

    /// <summary>
    /// Gets the names of the files in the directory the databases live in.
    /// </summary>
    protected IReadOnlyList<string> FilesOnDisk => [.. Directory.GetFiles(_directory).Select(Path.GetFileName).Order()!];
}
