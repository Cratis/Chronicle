// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_PrimaryKeyConstraints.when_reading_primary_key_columns;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.a_database_with_a_legacy_table
{
    protected override string CreateLegacyTable => "CREATE TABLE [legacy_models] ([Id] nvarchar(450) NOT NULL PRIMARY KEY, [name] nvarchar(max) NULL)";

    protected override string CreateCompositeTable => "CREATE TABLE [composite_models] ([first] nvarchar(200) NOT NULL, [second] nvarchar(200) NOT NULL, PRIMARY KEY ([first], [second]))";

    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        if (sql == CreateLegacyTable)
        {
            command.CommandText = "CREATE TABLE [legacy_models] ([Id] nvarchar(450) NOT NULL PRIMARY KEY, [name] nvarchar(max) NULL)";
        }
        else if (sql == CreateCompositeTable)
        {
            command.CommandText = "CREATE TABLE [composite_models] ([first] nvarchar(200) NOT NULL, [second] nvarchar(200) NOT NULL, PRIMARY KEY ([first], [second]))";
        }
        else
        {
            throw new InvalidOperationException("Unexpected table seed.");
        }

        await command.ExecuteNonQueryAsync();
    }

    protected override Task DropDatabase(string connectionString) => fixture.DropDatabase(connectionString);

    Task Because() => ReadLayouts();

    [Fact] void should_read_the_synthesized_key_spelled_as_the_catalog_spells_it() => _legacy.PrimaryKey.Single().ShouldEqual("Id");
    [Fact] void should_read_every_part_of_a_composite_key() => _composite.PrimaryKey.Order(StringComparer.Ordinal).ShouldEqual(["first", "second"]);
    [Fact] void should_read_no_key_for_a_table_that_does_not_exist() => _missing.PrimaryKey.ShouldBeEmpty();
    [Fact] void should_read_no_columns_for_a_table_that_does_not_exist() => _missing.Columns.ShouldBeEmpty();
    [Fact] void should_read_every_column() => _legacy.Columns.Order(StringComparer.Ordinal).ShouldEqual(["Id", "name"]);
}
