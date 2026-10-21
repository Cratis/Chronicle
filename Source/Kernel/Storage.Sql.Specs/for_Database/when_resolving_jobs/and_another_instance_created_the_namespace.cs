// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_jobs;

[Collection(PostgreSqlCollection.Name)]
public class and_another_instance_created_the_namespace(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _jobsDatabase;
    string _binding;

    async Task Establish()
    {
        Store = new string('s', 90);
        await new NamespaceStorage(Store, Database).Ensure(Namespace);
        _binding = await Binding(DatabaseKind.Namespace, Store, Namespace);
    }

    void Because()
    {
        var other = CreateDatabase();
        using var jobs = new Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext(other.GetJobsDbContextOptions(Store, Namespace));
        _jobsDatabase = jobs.Database.GetDbConnection().Database;
    }

    [Fact] void should_reload_the_namespace_binding_before_namespace_migration() => _jobsDatabase.ShouldEqual(_binding);
}
