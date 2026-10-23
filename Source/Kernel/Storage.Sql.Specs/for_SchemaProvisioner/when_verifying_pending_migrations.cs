// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_verifying_pending_migrations : given.a_schema_provisioner
{
    SchemaReport _report;
    string _migration;
    bool _stillPending;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        await using var cluster = await _schemaDatabase.Cluster();
        _migration = (await cluster.DbContext.Database.GetAppliedMigrationsAsync()).Last();
        await cluster.DbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM __EFMigrationsHistory WHERE MigrationId = {_migration}");
    }

    async Task Because()
    {
        _report = await _provisioner.Verify(Scope);
        await using var cluster = await _schemaDatabase.Cluster();
        _stillPending = !(await cluster.DbContext.Database.GetAppliedMigrationsAsync()).Contains(_migration);
    }

    [Fact] void should_report_the_pending_migration() => _report.Scopes.SelectMany(scope => scope.Pending).ShouldContain(_migration);
    [Fact] void should_not_apply_the_pending_migration() => _stillPending.ShouldBeTrue();
}
