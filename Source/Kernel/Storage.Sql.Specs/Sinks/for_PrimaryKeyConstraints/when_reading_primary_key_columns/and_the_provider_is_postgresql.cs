// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_PrimaryKeyConstraints.when_reading_primary_key_columns;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.a_database_with_a_legacy_table
{
    protected override string CreateLegacyTable => "CREATE TABLE \"legacy_models\" (\"Id\" text NOT NULL PRIMARY KEY, \"ID\" text NULL, \"name\" text NULL)";

    protected override string CreateCompositeTable => "CREATE TABLE \"composite_models\" (\"first\" text NOT NULL, \"second\" text NOT NULL, PRIMARY KEY (\"first\", \"second\"))";

    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override async Task Execute(string connectionString, string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    protected override Task DropDatabase(string connectionString) => fixture.DropDatabase(connectionString);

    Task Because() => ReadLayouts();

    [Fact] void should_read_the_synthesized_key_spelled_as_the_catalog_spells_it() => _legacy.PrimaryKey.Single().ShouldEqual("Id");
    [Fact] void should_read_every_part_of_a_composite_key() => _composite.PrimaryKey.Order(StringComparer.Ordinal).ShouldEqual(["first", "second"]);
    [Fact] void should_read_no_key_for_a_table_that_does_not_exist() => _missing.PrimaryKey.ShouldBeEmpty();
    [Fact] void should_read_no_columns_for_a_table_that_does_not_exist() => _missing.Columns.ShouldBeEmpty();
    [Fact] void should_read_every_column() => _legacy.Columns.Order(StringComparer.Ordinal).ShouldEqual(["ID", "Id", "name"]);
}
