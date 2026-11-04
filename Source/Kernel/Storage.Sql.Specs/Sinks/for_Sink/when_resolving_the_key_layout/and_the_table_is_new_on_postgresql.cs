// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout;

[Collection(PostgreSqlCollection.Name)]
public class and_the_table_is_new_on_postgresql(PostgreSqlFixture fixture) : given.a_sink_on_a_database_provider
{
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    protected override async Task Execute(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(_harness.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    protected override async Task<string[]> ReadKeys(string column)
    {
        await using var dataSource = NpgsqlDataSource.Create(_harness.ConnectionString);
        await using var command = dataSource.CreateCommand($"SELECT \"{column}\" FROM \"{Table}\" ORDER BY 1");
        await using var reader = await command.ExecuteReaderAsync();
        var keys = new List<string>();
        while (await reader.ReadAsync())
        {
            keys.Add(reader.GetString(0));
        }

        return [.. keys];
    }

    Task Because() => WriteAndReadLayout();

    [Fact] void should_create_a_single_key_column_spelled_as_the_identifier() => _layout.PrimaryKey.Single().ShouldEqual("ID");
    [Fact] void should_not_create_a_synthesized_key_column() => _layout.Columns.ShouldNotContain("Id");
    [Fact] void should_store_the_keys_in_the_identifier_column() => _keys.ShouldEqual(["one", "two"]);
    [Fact] void should_read_the_layout_once() => _layoutReads.ShouldEqual(1);
}
