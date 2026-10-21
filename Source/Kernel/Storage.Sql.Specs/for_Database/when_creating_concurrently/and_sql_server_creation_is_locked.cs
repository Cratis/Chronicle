// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_creating_concurrently;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_creation_is_locked(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    Exception _contenderFailure;
    Exception _afterRelease;

    async Task Because()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        var resource = DatabaseSchemaLock.ResourceFor(builder.InitialCatalog);
        builder.InitialCatalog = "master";
        await using var contender = new SqlConnection(builder.ConnectionString);
        await contender.OpenAsync();
        await SqlServerDatabaseCreation.RunWithLock(
            ConnectionString,
            _ => Task.CompletedTask,
            async () => _contenderFailure = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(contender, resource, TimeSpan.Zero)));
        _afterRelease = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(contender, resource, TimeSpan.Zero));
        if (_afterRelease is null)
        {
            await SqlServerDatabaseSchemaLock.Instance.Release(contender, resource);
        }
    }

    [Fact] void should_block_other_processes_before_the_target_is_opened() => _contenderFailure.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
    [Fact] void should_release_the_creation_lock() => _afterRelease.ShouldBeNull();
}
