// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SqlServerDatabaseCreation.when_enabling_read_committed_snapshot;

[Collection(SqlServerCollection.Name)]
public class and_it_is_already_on(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    SqlConnection _holder;
    object _held;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        var database = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
        await SqlServerSpecs.Execute(_connectionString, $"ALTER DATABASE [{database}] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE");
        _holder = new SqlConnection(SqlServerSpecs.Unpooled(_connectionString));
        await _holder.OpenAsync();
    }

    async Task Because()
    {
        await SqlServerReadCommittedSnapshot.Enable(_connectionString, null, NullLogger.Instance);
        await using var command = _holder.CreateCommand();
        command.CommandText = "SELECT 1";
        _held = await command.ExecuteScalarAsync();
    }

    async Task Destroy()
    {
        await _holder.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }

    [Fact] void should_not_evict_the_idle_session() => _held.ShouldEqual(1);
}
