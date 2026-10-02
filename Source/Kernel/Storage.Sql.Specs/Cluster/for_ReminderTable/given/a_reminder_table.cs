// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.given;

/// <summary>
/// Sets up a <see cref="ReminderTable"/> backed by an in-memory SQLite cluster database that keeps a single
/// connection open so every context the table creates sees the same data.
/// </summary>
public class a_reminder_table : Specification
{
    protected SqliteConnection _connection;
    protected IReminderTable _table;
    protected IDatabase _database;
    ServiceProvider _serviceProvider;

    async Task Establish()
    {
        _connection = new SqliteConnection($"Data Source=reminders-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        var options = new ChronicleOptions
        {
            Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _connection.ConnectionString }
        };
        _database = new Database(
            _serviceProvider,
            Options.Create(options),
            Substitute.For<IEventSequenceMigrator>(),
            Substitute.For<IUniqueConstraintMigrator>(),
            Substitute.For<IReadModelMigrator>());
        await using var scope = await _database.Cluster();

        _table = new ReminderTable(_database);
    }

    void Destroy()
    {
        _connection.Dispose();
        _serviceProvider.Dispose();
    }

    protected static ReminderEntry CreateEntry(GrainId grainId, string reminderName = "retry") => new()
    {
        GrainId = grainId,
        ReminderName = reminderName,
        StartAt = DateTime.UtcNow,
        Period = TimeSpan.FromMinutes(1)
    };

    protected ClusterDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ClusterDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new ClusterDbContext(options);
    }
}
