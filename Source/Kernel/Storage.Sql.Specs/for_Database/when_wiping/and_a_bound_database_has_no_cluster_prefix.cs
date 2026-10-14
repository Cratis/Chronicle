// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_wiping;

[Collection(PostgreSqlCollection.Name)]
public class and_a_bound_database_has_no_cluster_prefix(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _target;
    string _binding;
    long _rows;
    DbContextOptions<Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext> _before;
    DbContextOptions<Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext> _after;

    async Task Establish()
    {
        _target = $"operator_{Guid.NewGuid():N}";
        await CreateLegacyDatabase(_target);
        await Execute(ConnectionFor(_target), "CREATE TABLE payload (id int); INSERT INTO payload VALUES (1)");
        await using var cluster = await Database.Cluster();
        cluster.DbContext.DatabaseNames.Add(new DatabaseNameBinding { Kind = DatabaseKind.Namespace, EventStore = Store, Namespace = Namespace, PhysicalName = _target });
        await cluster.DbContext.SaveChangesAsync();
        _before = Database.GetJobsDbContextOptions(Store, Namespace);
    }

    async Task Because()
    {
        await Database.Wipe();
        _binding = await Binding(DatabaseKind.Namespace, Store, Namespace);
        _after = Database.GetJobsDbContextOptions(Store, Namespace);
        await using var connection = new NpgsqlConnection(ConnectionFor(_target));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM payload", connection);
        _rows = (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact] void should_keep_the_binding() => _binding.ShouldEqual(_target);
    [Fact] void should_reach_the_operator_bound_database() => _rows.ShouldEqual(0);
    [Fact] void should_reset_jobs_storage() => JobsStorage.Received(1).Reset();
    [Fact] void should_clear_cached_jobs_options() => _after.ShouldNotBeSame(_before);
}
#endif
