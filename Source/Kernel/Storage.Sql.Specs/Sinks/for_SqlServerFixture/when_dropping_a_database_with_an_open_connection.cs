// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerFixture;

[Collection(SqlServerCollection.Name)]
public class when_dropping_a_database_with_an_open_connection(SqlServerFixture fixture) : Specification
{
    string _database;
    SqlConnection _connection;
    Exception? _exception;
    bool _exists;

    async Task Establish()
    {
        _database = await fixture.CreateDatabase();
        var unpooled = new SqlConnectionStringBuilder(_database) { Pooling = false }.ConnectionString;
        _connection = new SqlConnection(unpooled);
        await _connection.OpenAsync();
    }

    async Task Because()
    {
        _exception = await Catch.Exception(async () =>
        {
            await fixture.DropDatabase(_database);
            await fixture.FlushDrops();
        });
        _exists = await Exists(_database);
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_remove_the_database() => _exists.ShouldBeFalse();

    async Task Destroy() => await _connection.DisposeAsync();

    static async Task<bool> Exists(string connectionString)
    {
        var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END";
        command.Parameters.Add(new SqlParameter("@name", name));
        return (int)(await command.ExecuteScalarAsync())! == 1;
    }
}
