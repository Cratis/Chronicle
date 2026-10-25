// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_schema_modes_share_ef_services : given.a_schema_provisioner
{
    IModelCacheKeyFactory _migrateServices;
    IModelCacheKeyFactory _verifyServices;
    Exception _exception;
    bool _reusedServices = true;
    bool _retainedGuards = true;

    async Task Because()
    {
        await using var migrate = new JobsDbContext(Database.BuildOptions<JobsDbContext>(_services, _connectionString));
        await using var verify = new JobsDbContext(Database.BuildOptions<JobsDbContext>(CreateServices(SchemaMode.Verify), _connectionString));
        _migrateServices = migrate.GetService<IModelCacheKeyFactory>();
        _verifyServices = verify.GetService<IModelCacheKeyFactory>();
        _exception = await Catch.Exception(() => verify.Database.MigrateAsync());
        for (var i = 0; i < 25; i++)
        {
            var services = CreateServices(i % 2 == 0 ? SchemaMode.Migrate : SchemaMode.Verify);
            var guard = services.GetRequiredService<DynamicSchemaGuard>();
            await using var context = DynamicSchemaGuard.Attach(new JobsDbContext(Database.BuildOptions<JobsDbContext>(services, _connectionString)), guard);
            _reusedServices &= ReferenceEquals(context.GetService<IModelCacheKeyFactory>(), _migrateServices);
            _retainedGuards &= ReferenceEquals(DynamicSchemaGuard.For(context), guard);
        }
    }

    [Fact] void should_reuse_singleton_ef_services() => _verifyServices.ShouldBeSame(_migrateServices);
    [Fact] void should_reuse_services_across_more_than_twenty_distinct_runtime_guards() => _reusedServices.ShouldBeTrue();
    [Fact] void should_keep_each_contexts_runtime_guard() => _retainedGuards.ShouldBeTrue();
    [Fact] void should_still_enforce_the_context_verification_policy() => _exception.ShouldBeOfExactType<SchemaNotCurrent>();
}
