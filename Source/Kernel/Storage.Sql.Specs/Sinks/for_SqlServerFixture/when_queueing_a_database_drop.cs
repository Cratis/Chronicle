// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerFixture;

[Collection(SqlServerCollection.Name)]
public class when_queueing_a_database_drop(SqlServerFixture fixture) : Specification
{
    string _database;
    bool _queueingCompleted;
    bool _exists;

    async Task Establish() => _database = await fixture.CreateDatabase();

    async Task Because()
    {
        var queued = fixture.QueueDatabaseDrop(_database);
        _queueingCompleted = queued.IsCompletedSuccessfully;
        await queued;
        await fixture.FlushDrops();

        var name = new SqlConnectionStringBuilder(_database).InitialCatalog;
        var master = new SqlConnectionStringBuilder(_database) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END";
        command.Parameters.Add(new SqlParameter("@name", name));
        _exists = (int)(await command.ExecuteScalarAsync())! == 1;
    }

    [Fact] void should_complete_queueing_without_waiting_for_deletion() => _queueingCompleted.ShouldBeTrue();
    [Fact] void should_remove_the_database_when_deferred_cleanup_is_flushed() => _exists.ShouldBeFalse();
}
