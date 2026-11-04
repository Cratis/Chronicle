// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout;

/// <summary>
/// Earlier releases ignored an upper-case identifier: they created a synthesized <c language="csharp">Id</c> key beside a nullable <c language="csharp">ID</c> column.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture supplying the test database.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_an_earlier_release_created_the_table_on_postgresql(PostgreSqlFixture fixture) : given.a_sink_on_a_database_provider
{
    object? _existing;
    object? _updated;

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

    protected override Task Prepare() => Execute($"CREATE TABLE \"{Table}\" (\"Id\" text NOT NULL PRIMARY KEY, \"ID\" text NULL, \"name\" text NULL, \"__lastHandledEventSequenceNumber\" numeric(20,0) NULL, \"__initialized\" boolean NOT NULL DEFAULT false); INSERT INTO \"{Table}\" (\"Id\", \"name\", \"__initialized\") VALUES ('one', 'released', true)");

    async Task Because()
    {
        var key = new Concepts.Keys.Key("one", Properties.ArrayIndexers.NoIndexers);
        _existing = await _sink.FindOrDefault(key);
        await WriteAndReadLayout();
        _updated = await _sink.FindOrDefault(key);
        await Replay();
        await WriteAndReadLayout();
    }

    [Fact] void should_read_the_released_row_identifier() => ((IDictionary<string, object?>)_existing)["ID"].ShouldEqual("one");
    [Fact] void should_update_the_released_row() => ((IDictionary<string, object?>)_updated)["name"].ShouldEqual("third");
    [Fact] void should_keep_the_synthesized_key() => _layout.PrimaryKey.Single().ShouldEqual("Id");
    [Fact] void should_keep_the_identifier_as_a_regular_column() => _layout.Columns.ShouldContain("ID");
    [Fact] void should_store_one_row_per_key() => _keys.ShouldEqual(["one", "two"]);
    [Fact] void should_read_the_layout_once() => _layoutReads.ShouldEqual(1);
}
