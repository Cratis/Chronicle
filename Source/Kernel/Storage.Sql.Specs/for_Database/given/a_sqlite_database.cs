// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_sqlite_database : Specification
{
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    protected readonly IEventSequenceMigrator _eventSequenceMigrator = Substitute.For<IEventSequenceMigrator>();
    protected Database _database;
    protected string _connectionString;
    protected string _directory;
    readonly List<Task> _operations = [];
    ServiceProvider _services;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"chronicle-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "cluster.db"), Pooling = false }.ConnectionString;
        await using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
        }

        _services = new ServiceCollection().BuildServiceProvider();
        _database = CreateDatabase();
    }

    protected Database CreateDatabase() => new(
        _services,
        Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _connectionString } }),
        _eventSequenceMigrator,
        Substitute.For<IUniqueConstraintMigrator>(),
        Substitute.For<IReadModelMigrator>());

    protected NamespaceDbContext Context(string? connectionString = null, params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(connectionString ?? _connectionString)
            .AddConceptAsSupport()
            .AddInterceptors(interceptors)
            .Options);

    protected Task Migrate(NamespaceDbContext context, string key = "namespace", Database? database = null)
    {
        var migrate = typeof(Database).GetMethod("EnsureMigratedOnce", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)migrate.Invoke(database ?? _database, [key, new SqlTarget(context.Database.GetConnectionString()!, string.Empty, null), context])!;
    }

    protected Task StartMigration(string? connectionString = null, string key = "namespace", Database? database = null, params IInterceptor[] interceptors) =>
        Track(MigrateInOwnContext(connectionString, key, database, interceptors));

    protected Task Track(Task operation)
    {
        _operations.Add(operation);
        return operation;
    }

    protected Task CompleteOperations() => Task.WhenAll(_operations).WaitAsync(Deadline);

    async Task MigrateInOwnContext(string? connectionString, string key, Database? database, IInterceptor[] interceptors)
    {
        await using var context = Context(connectionString, interceptors);
        await Migrate(context, key, database);
    }

    protected async Task<string[]> PendingMigrations()
    {
        await using var context = Context();
        return (await context.Database.GetPendingMigrationsAsync()).ToArray();
    }

    async Task Destroy()
    {
        await CompleteOperations();
        await _services.DisposeAsync();
        Directory.Delete(_directory, recursive: true);
    }
}
