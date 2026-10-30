// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerFixture;

[Collection(SqlServerCollection.Name)]
public class when_dropping_a_database(SqlServerFixture fixture) : Specification
{
    string _dropped;
    string _next;
    bool _droppedStillExists;
    bool _nextHasTables;
    bool _nextExists;

    async Task Because()
    {
        _dropped = await fixture.CreateDatabase();
        await Execute(_dropped, "CREATE TABLE written_by_the_dropped_database (id int)");
        await fixture.DropDatabase(_dropped);
        _next = await fixture.CreateDatabase();
        _droppedStillExists = await Exists(_dropped);
        _nextExists = await Exists(_next);
        _nextHasTables = (await Scalar(_next, "SELECT COUNT(*) FROM sys.tables")) > 0;
    }

    [Fact] void should_remove_the_dropped_database_before_returning() => _droppedStillExists.ShouldBeFalse();
    [Fact] void should_give_the_next_specification_a_different_database() => _next.ShouldNotEqual(_dropped);
    [Fact] void should_keep_the_next_database() => _nextExists.ShouldBeTrue();
    [Fact] void should_give_the_next_specification_an_empty_database() => _nextHasTables.ShouldBeFalse();

    async Task Destroy() => await fixture.DropDatabase(_next);

    static async Task<bool> Exists(string connectionString)
    {
        var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString;
        return await Scalar(master, $"SELECT CASE WHEN DB_ID('{name}') IS NULL THEN 0 ELSE 1 END") == 1;
    }

    static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Fixed statements and fixture-generated identifiers only.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    static async Task<int> Scalar(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Fixed statements and fixture-generated identifiers only.
        command.CommandText = sql;
#pragma warning restore CA2100
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
