// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicIndexShape;

[Collection(PostgreSqlCollection.Name)]
public class when_comparing_postgresql_catalog_shapes(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    static readonly string _longIndexName = "catalog_" + new string('é', 40) + "_value";
    bool _uniqueValue;
    bool _partialWasAccepted;
    bool _missingWasAccepted;
    Exception? _matching;
    Exception? _wrongKeys;
    Exception? _wrongUniqueness;
    Exception? _partial;
    Exception? _matchingFilter;
    Exception? _wrongFilter;
    IReadOnlyList<string> _tableDifferences;
    bool _truncatedExists;
    bool _truncatedUniqueValue;
    bool _truncatedMissingWasAccepted;
    Exception? _truncatedMatching;
    Exception? _truncatedWrongKeys;
    Exception? _truncatedMissing;

    async Task Because()
    {
        await using var context = new DbContext(new DbContextOptionsBuilder().UseNpgsql(ConnectionString).WithoutServiceProviderCaching().Options);
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
        _matchingFilter = await Inspect(context, ["Value"], unique: true, filter: "\"Other\" > 0");
        _wrongFilter = await Inspect(context, ["Value"], unique: true, filter: "\"Other\" > 1");
        await context.Database.ExecuteSqlRawAsync("DROP INDEX catalog_value");
        _missingWasAccepted = await DynamicIndexShape.Exists(context, "catalog_shape", "catalog_value");
        var migrator = new TableMigrator<DbContext>(NullLogger<TableMigrator<DbContext>>.Instance);
        var indexes = new MigrationBuilder(context.Database.ProviderName);
        indexes.CreateIndex(_longIndexName, "catalog_shape", "Value", unique: true);
        await migrator.ExecuteMigrationOperations(context, indexes);
        _truncatedExists = await DynamicIndexShape.Exists(context, "catalog_shape", _longIndexName);
        _truncatedUniqueValue = await DynamicIndexShape.IsUniqueValue(context, "catalog_shape", _longIndexName);
        _truncatedMatching = await Inspect(context, ["Value"], unique: true, name: _longIndexName);
        _truncatedWrongKeys = await Inspect(context, ["Other"], unique: true, name: _longIndexName);
        indexes = new MigrationBuilder(context.Database.ProviderName);
        indexes.DropIndex(_longIndexName, "catalog_shape");
        await migrator.ExecuteMigrationOperations(context, indexes);
        _truncatedMissingWasAccepted = await DynamicIndexShape.Exists(context, "catalog_shape", _longIndexName);
        _truncatedMissing = await Inspect(context, ["Value"], unique: true, name: _longIndexName);
    }

    static async Task<Exception?> Inspect(DbContext context, string[] columns, bool unique, string? filter = null, string name = "catalog_value")
    {
        using var inspection = DynamicSchemaVerification.Begin();
        await DynamicIndexShape.Verify(context, new() { Name = name, Table = "catalog_shape", Columns = columns, IsUnique = unique, Filter = filter });
        return Catch.Exception(() => DynamicSchemaVerification.Complete("test"));
    }

    [Fact] void should_accept_equivalent_postgresql_column_types() => _tableDifferences.ShouldBeEmpty();
    [Fact] void should_recognize_a_unique_value_index() => _uniqueValue.ShouldBeTrue();
    [Fact] void should_accept_the_matching_index() => _matching.ShouldBeNull();
    [Fact] void should_reject_different_keys() => _wrongKeys.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_reject_different_uniqueness() => _wrongUniqueness.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_accept_a_partial_unique_value_index() => _partialWasAccepted.ShouldBeFalse();
    [Fact] void should_accept_the_expected_filter() => _matchingFilter.ShouldBeNull();
    [Fact] void should_reject_a_different_filter() => _wrongFilter.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_report_partial_index_filtering() => _partial.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_accept_a_missing_index() => _missingWasAccepted.ShouldBeFalse();
    [Fact] void should_find_a_natively_byte_truncated_index_name() => _truncatedExists.ShouldBeTrue();
    [Fact] void should_recognize_a_truncated_unique_value_index() => _truncatedUniqueValue.ShouldBeTrue();
    [Fact] void should_verify_a_truncated_index_by_its_exact_keys() => _truncatedMatching.ShouldBeNull();
    [Fact] void should_still_reject_wrong_keys_for_a_truncated_index() => _truncatedWrongKeys.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_find_a_dropped_truncated_index() => _truncatedMissingWasAccepted.ShouldBeFalse();
    [Fact] void should_report_a_missing_truncated_index() => _truncatedMissing.ShouldBeOfExactType<SchemaNotCurrent>();
}
