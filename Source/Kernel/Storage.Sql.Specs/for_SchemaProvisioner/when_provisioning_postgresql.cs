// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

[Collection(PostgreSqlCollection.Name)]
public class when_provisioning_postgresql(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    SchemaReport _report;
    Exception _startupFailure;
    string[] _pendingJobs;

    async Task Because()
    {
        var provisioner = new SchemaProvisioner(Database, NullLogger<SchemaProvisioner>.Instance);
        var scope = new SchemaScope([Store], ["production"]);
        await provisioner.Migrate(scope);
        await provisioner.Migrate(scope);
        _report = await provisioner.Verify(scope);
        await using var services = given.schema_services.Create(new ChronicleOptions
        {
            Storage = new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString, SchemaMode = SchemaMode.Verify }
        });
        var database = services.GetRequiredService<Database>();
        _startupFailure = await Catch.Exception(() => new ClusterDbContextMigrator(database).Execute(CancellationToken.None));
        await using var jobs = await database.SchemaContext<Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext>(new DatabaseIdentity(DatabaseKind.Namespace, Store, "production"), static options => new Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext(options), true);
        _pendingJobs = (await jobs.Database.GetPendingMigrationsAsync()).ToArray();
    }

    [Fact] void should_verify_every_provisioned_scope() => _report.IsCurrent.ShouldBeTrue();
    [Fact] void should_start_with_verify_policy() => _startupFailure.ShouldBeNull();
    [Fact] void should_prepare_the_jobs_history() => _pendingJobs.ShouldBeEmpty();
    [Fact] void should_include_the_explicit_namespace() => _report.Scopes.Any(scope => scope.Scope.Contains("production", StringComparison.Ordinal)).ShouldBeTrue();
}
