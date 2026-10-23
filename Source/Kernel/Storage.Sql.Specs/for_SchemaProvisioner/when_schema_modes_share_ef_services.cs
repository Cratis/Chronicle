// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_schema_modes_share_ef_services : given.a_schema_provisioner
{
    IModelCacheKeyFactory _migrateServices;
    IModelCacheKeyFactory _verifyServices;
    Exception _exception;

    async Task Because()
    {
        await using var migrate = new JobsDbContext(Database.BuildOptions<JobsDbContext>(_services, _connectionString));
        await using var verify = new JobsDbContext(Database.BuildOptions<JobsDbContext>(CreateServices(SchemaMode.Verify), _connectionString));
        _migrateServices = migrate.GetService<IModelCacheKeyFactory>();
        _verifyServices = verify.GetService<IModelCacheKeyFactory>();
        _exception = await Catch.Exception(() => verify.Database.MigrateAsync());
    }

    [Fact] void should_reuse_singleton_ef_services() => _verifyServices.ShouldBeSame(_migrateServices);
    [Fact] void should_still_enforce_the_context_verification_policy() => _exception.ShouldBeOfExactType<SchemaNotCurrent>();
}
