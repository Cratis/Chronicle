// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicIndexShape;

[Collection(PostgreSqlCollection.Name)]
public class when_comparing_postgresql_catalog_shapes(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    bool _uniqueValue;
    bool _partialWasAccepted;
    bool _missingWasAccepted;
    Exception? _matching;
    Exception? _wrongKeys;
    Exception? _wrongUniqueness;
    Exception? _partial;
    IReadOnlyList<string> _tableDifferences;

    async Task Because()
    {
        await using var context = new DbContext(new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE catalog_shape (\"Value\" integer NOT NULL PRIMARY KEY, \"Other\" integer NOT NULL, \"Label\" varchar(24) NOT NULL, \"Amount\" numeric(12,2) NOT NULL, \"Enabled\" boolean NOT NULL)");
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(
            "catalog_shape",
            columns: table => new
            {
                Value = table.Column<int>(type: "int4", nullable: false),
                Other = table.Column<int>(type: "integer", nullable: false),
                Label = table.Column<string>(type: "character varying(24)", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                Enabled = table.Column<bool>(type: "bool", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_shape", row => row.Value));
        _tableDifferences = await DynamicTableShape.Differences(context, migration.Operations.OfType<CreateTableOperation>().Single());
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX catalog_value ON catalog_shape (\"Value\")");
        _uniqueValue = await DynamicIndexShape.IsUniqueValue(context, "catalog_shape", "catalog_value");
        _matching = await Inspect(context, ["Value"], unique: true);
        _wrongKeys = await Inspect(context, ["Other"], unique: true);
        _wrongUniqueness = await Inspect(context, ["Value"], unique: false);
        await context.Database.ExecuteSqlRawAsync("DROP INDEX catalog_value");
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX catalog_value ON catalog_shape (\"Value\") WHERE \"Other\" > 0");
        _partialWasAccepted = await DynamicIndexShape.IsUniqueValue(context, "catalog_shape", "catalog_value");
        _partial = await Inspect(context, ["Value"], unique: true);
        await context.Database.ExecuteSqlRawAsync("DROP INDEX catalog_value");
        _missingWasAccepted = await DynamicIndexShape.Exists(context, "catalog_shape", "catalog_value");
    }

    static async Task<Exception?> Inspect(DbContext context, string[] columns, bool unique)
    {
        using var inspection = DynamicSchemaVerification.Begin();
        await DynamicIndexShape.Verify(context, new() { Name = "catalog_value", Table = "catalog_shape", Columns = columns, IsUnique = unique });
        return Catch.Exception(() => DynamicSchemaVerification.Complete("test"));
    }

    [Fact] void should_accept_equivalent_postgresql_column_types() => _tableDifferences.ShouldBeEmpty();
    [Fact] void should_recognize_a_unique_value_index() => _uniqueValue.ShouldBeTrue();
    [Fact] void should_accept_the_matching_index() => _matching.ShouldBeNull();
    [Fact] void should_reject_different_keys() => _wrongKeys.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_reject_different_uniqueness() => _wrongUniqueness.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_accept_a_partial_unique_value_index() => _partialWasAccepted.ShouldBeFalse();
    [Fact] void should_report_partial_index_filtering() => _partial.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_accept_a_missing_index() => _missingWasAccepted.ShouldBeFalse();
}
