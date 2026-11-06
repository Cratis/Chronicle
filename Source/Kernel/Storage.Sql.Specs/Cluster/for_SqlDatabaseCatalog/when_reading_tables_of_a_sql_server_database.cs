// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlDatabaseCatalog;

[Collection(SqlServerCollection.Name)]
public class when_reading_tables_of_a_sql_server_database(SqlServerFixture fixture) : Specification
{
    const int MaximumReads = 2000;

    string _connectionString;
    int _sessions;

    async Task Establish() => _connectionString = await fixture.CreateDatabase();

    async Task Because()
    {
        var database = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
        await new SqlDatabaseCatalog(_connectionString).Tables(database);

        var master = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master", Pooling = false }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE database_id = DB_ID(@name) AND session_id <> @@SPID";
        command.Parameters.AddWithValue("name", database);

        // Closing a connection returns before the server has ended its session, so a session that is going away is not
        // one that was left behind: only one that is still there after many reads of the session list is. Each read is a
        // round trip to the server, so the bound is a number of reads, not a duration.
        for (var read = 0; read < MaximumReads; read++)
        {
            _sessions = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (_sessions == 0)
            {
                break;
            }
        }
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    [Fact] void should_leave_no_session_in_the_database() => _sessions.ShouldEqual(0);
}
