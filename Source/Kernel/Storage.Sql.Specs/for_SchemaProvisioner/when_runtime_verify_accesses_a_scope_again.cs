// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_runtime_verify_accesses_a_scope_again : given.a_schema_provisioner
{
    Database _verifying;
    Exception _exception;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        _verifying = CreateServices(SchemaMode.Verify).GetRequiredService<Database>();
        await using (await _verifying.Namespace("orders", "production"))
        {
        }

        // A schema that is no longer current is only noticed by a check that actually reads history.
        await using var scope = await _schemaDatabase.Namespace("orders", "production");
        var migration = (await scope.DbContext.Database.GetAppliedMigrationsAsync()).Last();
        await scope.DbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM __EFMigrationsHistory WHERE MigrationId = {migration}");
    }

    async Task Because() => _exception = await Catch.Exception(async () =>
    {
        await using var scope = await _verifying.Namespace("orders", "production");
    });

    [Fact] void should_reuse_the_passing_verification() => _exception.ShouldBeNull();
}
