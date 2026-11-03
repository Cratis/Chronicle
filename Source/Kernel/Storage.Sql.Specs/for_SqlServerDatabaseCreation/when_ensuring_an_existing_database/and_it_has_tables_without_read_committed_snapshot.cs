// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SqlServerDatabaseCreation.when_ensuring_an_existing_database;

[Collection(SqlServerCollection.Name)]
public class and_it_has_tables_without_read_committed_snapshot(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    SqlConnection _holder;
    object _held;
    int _snapshotOn;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await SqlServerSpecs.Execute(_connectionString, "CREATE TABLE spec_table (id int)");
        _holder = new SqlConnection(SqlServerSpecs.Unpooled(_connectionString));
        await _holder.OpenAsync();
    }

    async Task Because()
    {
        await SqlServerReadCommittedSnapshot.WarnIfOff(_connectionString, null, NullLogger.Instance);
        _snapshotOn = await SqlServerSpecs.SnapshotOn(_connectionString);
        await using var command = _holder.CreateCommand();
        command.CommandText = "SELECT 1";
        _held = await command.ExecuteScalarAsync();
    }

    async Task Destroy()
    {
        await _holder.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }

    [Fact] void should_leave_read_committed_snapshot_off() => _snapshotOn.ShouldEqual(0);
    [Fact] void should_not_evict_the_idle_session() => _held.ShouldEqual(1);
}
