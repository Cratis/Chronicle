// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_jobs_migrations_are_called_directly_in_verify_mode : given.a_schema_provisioner
{
    Exception _asyncException;
    Exception _syncException;
    string _migration;
    string _connection;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        await using var scope = await _schemaDatabase.Namespace("orders", "production");
        _connection = scope.DbContext.Database.GetConnectionString();
        await using var jobs = new JobsDbContext(Database.BuildOptions<JobsDbContext>(_services, _connection));
        _migration = jobs.Database.GetMigrations().First();
        await scope.DbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM __EFMigrationsHistory WHERE MigrationId = {_migration}");
    }

    async Task Because()
    {
        var services = CreateServices(SchemaMode.Verify);
        await using var context = new JobsDbContext(Database.BuildOptions<JobsDbContext>(services, _connection));
        _asyncException = await Catch.Exception(() => context.Database.MigrateAsync());
        _syncException = Catch.Exception(context.Database.Migrate);
    }

    [Fact] void should_gate_async_migration() => _asyncException.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_gate_sync_migration() => _syncException.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_name_the_pending_jobs_migration() => ((SchemaNotCurrent)_asyncException).Pending.ShouldContain(_migration);
}
