// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_concurrently;

[Collection(SqlServerCollection.Name)]
public class and_another_sql_server_creator_wins(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    string _connectionString;
    ClusterDbContext _context;
    bool _initiallyExisted;
    string[] _pending;

    async Task Establish()
    {
        _connectionString = await Fixture.CreateDatabase();
        await Fixture.DropDatabase(_connectionString);
        _context = new(new DbContextOptionsBuilder<ClusterDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().Options);
        _initiallyExisted = await _context.GetService<IRelationalDatabaseCreator>().ExistsAsync();

        // Another kernel creates it without clearing this kernel's cached missing-database login failure.
        var unpooled = new SqlConnectionStringBuilder(_connectionString) { Pooling = false }.ConnectionString;
        await using var winner = new ClusterDbContext(new DbContextOptionsBuilder<ClusterDbContext>().UseSqlServer(unpooled).AddConceptAsSupport().Options);
        await winner.GetService<IRelationalDatabaseCreator>().CreateAsync();
    }

    async Task Because()
    {
        var migrate = typeof(Database).GetMethod("EnsureMigratedOnce", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)migrate.Invoke(Database, ["cluster-race", new SqlTarget(_connectionString, string.Empty, null), _context])!).WaitAsync(TimeSpan.FromSeconds(30));
        _pending = (await _context.Database.GetPendingMigrationsAsync()).ToArray();
    }

    [Fact] void should_start_with_a_missing_database() => _initiallyExisted.ShouldBeFalse();
    [Fact] void should_complete_migrations_after_losing_creation() => _pending.ShouldBeEmpty();

    async Task Destroy()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }

        if (_connectionString is not null)
        {
            await Fixture.DropDatabase(_connectionString);
        }
    }
}
