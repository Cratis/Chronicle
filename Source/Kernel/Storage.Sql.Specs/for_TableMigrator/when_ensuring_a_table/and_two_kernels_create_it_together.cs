// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_ensuring_a_table;

[Collection(PostgreSqlCollection.Name)]
public class and_two_kernels_create_it_together(PostgreSqlFixture fixture) : given.a_postgresql_table_migrator(fixture)
{
    readonly List<SchemaLockDbContext> _kernels = [];
    Exception _error;
    int _creations;

    void Establish()
    {
        // Distinct connection strings give each simulated kernel its own in-process cache and semaphore,
        // so only the lock in the database keeps them apart.
        foreach (var kernel in new[] { "first-kernel", "second-kernel" })
        {
            var connectionString = new NpgsqlConnectionStringBuilder(ConnectionString) { ApplicationName = kernel }.ConnectionString;
            _kernels.Add(new SchemaLockDbContext(new DbContextOptionsBuilder<SchemaLockDbContext>().UseNpgsql(connectionString).Options));
        }
    }

    async Task Because() => _error = await Catch.Exception(() => Task.WhenAll(_kernels.Select(kernel => Task.Run(() => Migrator.EnsureTableMigrated(Table, kernel, async (context, _) =>
    {
        Interlocked.Increment(ref _creations);

        // Long enough for the other kernel to pass its existence check, were nothing keeping them apart.
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await CreateTable(context);
    })))));

    async Task Destroy()
    {
        foreach (var kernel in _kernels)
        {
            await kernel.DisposeAsync();
        }
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_create_the_table_once() => _creations.ShouldEqual(1);
    [Fact] async Task should_have_the_table() => (await TableExists()).ShouldBeTrue();
}
