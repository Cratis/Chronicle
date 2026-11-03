// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_PrimaryKeyConstraints.when_reading_primary_key_columns;

public class and_the_provider_is_sqlite : given.a_database_with_a_legacy_table
{
    string _path = Path.Combine(Path.GetTempPath(), $"chronicle-keys-{Guid.NewGuid():N}.db");

    protected override string CreateLegacyTable => "CREATE TABLE \"legacy_models\" (\"Id\" TEXT NOT NULL PRIMARY KEY, \"name\" TEXT NULL)";

    protected override string CreateCompositeTable => "CREATE TABLE \"composite_models\" (\"first\" TEXT NOT NULL, \"second\" TEXT NOT NULL, PRIMARY KEY (\"first\", \"second\"))";

    protected override Task<string> CreateDatabase() => Task.FromResult(new SqliteConnectionStringBuilder { DataSource = _path, Pooling = false }.ToString());

    protected override async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        if (sql == CreateLegacyTable)
        {
            command.CommandText = "CREATE TABLE \"legacy_models\" (\"Id\" TEXT NOT NULL PRIMARY KEY, \"name\" TEXT NULL)";
        }
        else if (sql == CreateCompositeTable)
        {
            command.CommandText = "CREATE TABLE \"composite_models\" (\"first\" TEXT NOT NULL, \"second\" TEXT NOT NULL, PRIMARY KEY (\"first\", \"second\"))";
        }
        else
        {
            throw new InvalidOperationException("Unexpected table seed.");
        }

        await command.ExecuteNonQueryAsync();
    }

    Task Because() => ReadLayouts();

    [Fact] void should_read_the_synthesized_key_spelled_as_the_catalog_spells_it() => _legacy.PrimaryKey.Single().ShouldEqual("Id");
    [Fact] void should_read_every_part_of_a_composite_key() => _composite.PrimaryKey.Order(StringComparer.Ordinal).ShouldEqual(["first", "second"]);
    [Fact] void should_read_no_key_for_a_table_that_does_not_exist() => _missing.PrimaryKey.ShouldBeEmpty();
    [Fact] void should_read_no_columns_for_a_table_that_does_not_exist() => _missing.Columns.ShouldBeEmpty();
    [Fact] void should_read_every_column() => _legacy.Columns.Order(StringComparer.Ordinal).ShouldEqual(["Id", "name"]);

    protected override Task DropDatabase(string connectionString)
    {
        File.Delete(_path);
        return Task.CompletedTask;
    }
}
