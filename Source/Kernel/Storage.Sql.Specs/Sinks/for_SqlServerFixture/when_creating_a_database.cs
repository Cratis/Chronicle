// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerFixture;

[Collection(SqlServerCollection.Name)]
public class when_creating_a_database(SqlServerFixture fixture) : Specification
{
    string _first;
    string _second;
    int _firstTables;
    int _secondTables;

    async Task Because()
    {
        _first = await fixture.CreateDatabase();
        _second = await fixture.CreateDatabase();
        _firstTables = await Scalar(_first, "SELECT COUNT(*) FROM sys.tables");
        _secondTables = await Scalar(_second, "SELECT COUNT(*) FROM sys.tables");
    }

    [Fact] void should_give_each_specification_a_different_database() => _second.ShouldNotEqual(_first);
    [Fact] void should_give_an_empty_first_database() => _firstTables.ShouldEqual(0);
    [Fact] void should_give_an_empty_second_database() => _secondTables.ShouldEqual(0);

    async Task Destroy()
    {
        await fixture.DropDatabase(_first);
        await fixture.DropDatabase(_second);
    }

    static async Task<int> Scalar(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Fixed statements only.
        command.CommandText = sql;
#pragma warning restore CA2100
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
