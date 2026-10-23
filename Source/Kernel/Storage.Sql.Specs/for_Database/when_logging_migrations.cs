// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database;

public class when_logging_migrations : given.a_sqlite_database
{
    readonly given.MigrationLogger _logger = new();
    Dictionary<string, object?>[] _events;
    string[] _applied;
    int _afterSecondMigration;

    async Task Because()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connectionString).AddConceptAsSupport();
        SqlMigrationTelemetry.Configure(builder, _connectionString, _logger);
        await using var context = new NamespaceDbContext(builder.Options);
        await Migrate(context);
        _applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        _events = _logger.Entries.ToArray();
        await Migrate(context, "another-cache-key");
        _afterSecondMigration = _logger.Entries.Count;
    }

    [Fact] void should_have_run_migrations() => _applied.Length.ShouldBeGreaterThan(0);
    [Fact] void should_record_each_migration() => _events.Select(entry => entry["Migration"]).ShouldEqual(_applied.Cast<object?>());
    [Fact] void should_record_the_node() => _events.All(entry => Equals(entry["Node"], Environment.MachineName)).ShouldBeTrue();
    [Fact] void should_record_the_process() => _events.All(entry => Equals(entry["ProcessId"], Environment.ProcessId)).ShouldBeTrue();
    [Fact] void should_record_only_the_store_name() => _events.All(entry => Equals(entry["Store"], "cluster.db")).ShouldBeTrue();
    [Fact] void should_not_report_already_applied_migrations() => _afterSecondMigration.ShouldEqual(_events.Length);
}
