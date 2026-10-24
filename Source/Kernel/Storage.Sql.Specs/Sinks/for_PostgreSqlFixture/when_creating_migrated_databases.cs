// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_PostgreSqlFixture;

[Collection(PostgreSqlCollection.Name)]
public class when_creating_migrated_databases(PostgreSqlFixture fixture) : Specification
{
    readonly List<string> _databases = [];
    string[] _concurrentDatabases;
    string[] _namespaceMigrations;
    string[] _eventStoreMigrations;
    bool _namespaceHasPendingMigrations;
    bool _eventStoreHasPendingMigrations;
    int _secondDatabaseRows;
    int _laterDatabaseRows;

    async Task Because()
    {
        _concurrentDatabases = await Task.WhenAll(
            fixture.CreateMigratedDatabase<NamespaceDbContext>(),
            fixture.CreateMigratedDatabase<NamespaceDbContext>());
        _databases.AddRange(_concurrentDatabases);
        await using (var first = NamespaceContext(_concurrentDatabases[0]))
        {
            _namespaceMigrations = (await first.Database.GetAppliedMigrationsAsync()).ToArray();
            _namespaceHasPendingMigrations = (await first.Database.GetPendingMigrationsAsync()).Any();
            first.Observers.Add(new() { Id = "first-database-only" });
            await first.SaveChangesAsync();
        }

        var laterDatabase = await fixture.CreateMigratedDatabase<NamespaceDbContext>();
        _databases.Add(laterDatabase);
        await fixture.DropDatabase(_concurrentDatabases[0]);
        _databases.Remove(_concurrentDatabases[0]);
        await using (var second = NamespaceContext(_concurrentDatabases[1]))
        {
            _secondDatabaseRows = await second.Observers.CountAsync();
        }
        await using (var later = NamespaceContext(laterDatabase))
        {
            _laterDatabaseRows = await later.Observers.CountAsync();
        }

        var eventStoreDatabase = await fixture.CreateMigratedDatabase<EventStoreDbContext>();
        _databases.Add(eventStoreDatabase);
        await using var eventStore = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseNpgsql(eventStoreDatabase).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
        _eventStoreMigrations = (await eventStore.Database.GetAppliedMigrationsAsync()).ToArray();
        _eventStoreHasPendingMigrations = (await eventStore.Database.GetPendingMigrationsAsync()).Any();
    }

    [Fact] void should_give_concurrent_callers_different_databases() => _concurrentDatabases.Distinct().Count().ShouldEqual(2);
    [Fact] void should_copy_the_namespace_migrations() => _namespaceMigrations.ShouldNotBeEmpty();
    [Fact] void should_not_leave_namespace_migrations_pending() => _namespaceHasPendingMigrations.ShouldBeFalse();
    [Fact] void should_keep_the_other_copy_empty_after_writing_and_dropping_one_copy() => _secondDatabaseRows.ShouldEqual(0);
    [Fact] void should_not_copy_specification_data_back_to_the_template() => _laterDatabaseRows.ShouldEqual(0);
    [Fact] void should_migrate_a_different_context_independently() => _eventStoreMigrations.Intersect(_namespaceMigrations).ShouldBeEmpty();
    [Fact] void should_copy_the_event_store_migrations() => _eventStoreMigrations.ShouldNotBeEmpty();
    [Fact] void should_not_leave_event_store_migrations_pending() => _eventStoreHasPendingMigrations.ShouldBeFalse();

    async Task Destroy()
    {
        foreach (var database in _databases)
        {
            await fixture.DropDatabase(database);
        }
    }

    static NamespaceDbContext NamespaceContext(string connectionString) => new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseNpgsql(connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
}
