// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_already_exists_has_no_pending_migrations(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    readonly given.an_already_exists_interceptor _failure = new();
    string _connection;
    Exception _error;
    string[] _pending;
    int _secondMigrationReads;

    async Task Establish()
    {
        await using var scope = await Database.Namespace(Store, Namespace);
        _connection = scope.DbContext.Database.GetConnectionString()!;
    }

    async Task Because()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>(SpecDbContextOptions.Kernel<NamespaceDbContext>(_connection))
            .AddInterceptors(_failure);
        await using var context = new NamespaceDbContext(builder.Options);

        // Inject a duplicate-object error while migrating an already complete, real PostgreSQL schema.
        // A fresh cache key exercises the same migration coordinator used by Namespace without changing its public API.
        var migrate = typeof(Database).GetMethod("EnsureMigratedOnce", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _error = await Catch.Exception(async () =>
        {
            await (Task)migrate.Invoke(Database, ["complete-history-probe", new SqlTarget(_connection, string.Empty, null), context])!;
            var reads = _failure.HistoryReads;
            await (Task)migrate.Invoke(Database, ["complete-history-probe", new SqlTarget(_connection, string.Empty, null), context])!;
            _secondMigrationReads = _failure.HistoryReads - reads;
        });
        _pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();
    }

    [Fact] void should_accept_the_complete_history() => _error.ShouldBeNull();
    [Fact] void should_have_no_pending_migrations() => _pending.ShouldBeEmpty();
    [Fact] void should_have_exercised_the_already_exists_path() => _failure.Failures.ShouldEqual(1);
    [Fact] void should_mark_the_database_migrated() => _secondMigrationReads.ShouldEqual(0);
}
