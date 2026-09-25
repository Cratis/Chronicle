// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.given;

/// <summary>
/// Sets up a <see cref="ReminderTable"/> backed by an in-memory SQLite cluster database that keeps a single
/// connection open so every context the table creates sees the same data.
/// </summary>
public class a_reminder_table : Specification
{
    protected SqliteConnection _connection;
    protected IDbContextFactory<ClusterDbContext> _dbContextFactory;
    protected IReminderTable _table;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        _dbContextFactory = Substitute.For<IDbContextFactory<ClusterDbContext>>();
        _dbContextFactory.CreateDbContext().Returns(_ => CreateContext());
        _dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(CreateContext()));

        _table = new ReminderTable(_dbContextFactory);
    }

    void Destroy() => _connection.Dispose();

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
