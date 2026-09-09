// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using MutationMigration = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.Migrations.v16_45_0;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_v16_45_0;

public class when_building_provider_operations : Specification
{
    const string PostgreSql = "Npgsql.EntityFrameworkCore.PostgreSQL";
    const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
    const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";

    IReadOnlyDictionary<string, (string Ordinal, string Text, string Id)> _types;
    IReadOnlyDictionary<string, string> _postgreSqlDigestTypes;
    IReadOnlyDictionary<string, string> _sqlServerDigestTypes;

    void Because()
    {
        _types = new Dictionary<string, (string, string, string)>
        {
            [PostgreSql] = ColumnTypesFor(PostgreSql),
            [SqlServer] = ColumnTypesFor(SqlServer),
            [Sqlite] = ColumnTypesFor(Sqlite)
        };

        // maxLength is not carried on AddColumnOperation.MaxLength by the shared StringColumn
        // helper - it is baked directly into the provider-native column type string instead
        // (VARCHAR(n) / NVARCHAR(n)), and SQLite has no length-bounded text type at all
        // (StringColumn always emits "TEXT" there, regardless of maxLength), so the 32-byte
        // hex sizing is only observable on the length-aware providers.
        _postgreSqlDigestTypes = DigestColumnTypesFor(PostgreSql);
        _sqlServerDigestTypes = DigestColumnTypesFor(SqlServer);
    }

    [Fact] void should_use_postgresql_native_types() => _types[PostgreSql].ShouldEqual(("BIGINT", "TEXT", "UUID"));
    [Fact] void should_use_sql_server_native_types() => _types[SqlServer].ShouldEqual(("BIGINT", "NVARCHAR(MAX)", "UNIQUEIDENTIFIER"));
    [Fact] void should_use_sqlite_canonical_text_mutation_ids() => _types[Sqlite].ShouldEqual(("INTEGER", "TEXT", "TEXT"));
    [Fact] void should_size_the_active_definition_digest_column_to_a_32_byte_hex_string_on_postgresql() => _postgreSqlDigestTypes["ActiveDefinitionDigestV1"].ShouldEqual("VARCHAR(64)");
    [Fact] void should_size_the_history_definition_digest_column_to_a_32_byte_hex_string_on_postgresql() => _postgreSqlDigestTypes["DefinitionDigestV1"].ShouldEqual("VARCHAR(64)");
    [Fact] void should_size_the_history_receipt_digest_column_to_a_32_byte_hex_string_on_postgresql() => _postgreSqlDigestTypes["ReceiptDigestV1"].ShouldEqual("VARCHAR(64)");
    [Fact] void should_size_the_active_definition_digest_column_to_a_32_byte_hex_string_on_sql_server() => _sqlServerDigestTypes["ActiveDefinitionDigestV1"].ShouldEqual("NVARCHAR(64)");
    [Fact] void should_size_the_history_definition_digest_column_to_a_32_byte_hex_string_on_sql_server() => _sqlServerDigestTypes["DefinitionDigestV1"].ShouldEqual("NVARCHAR(64)");
    [Fact] void should_size_the_history_receipt_digest_column_to_a_32_byte_hex_string_on_sql_server() => _sqlServerDigestTypes["ReceiptDigestV1"].ShouldEqual("NVARCHAR(64)");

    static (string Ordinal, string Text, string Id) ColumnTypesFor(string provider)
    {
        var migrationBuilder = new MigrationBuilder(provider);
        new ExposedMigration().Apply(migrationBuilder);
        var heads = migrationBuilder.Operations
            .OfType<CreateTableOperation>()
            .Single(_ => _.Name == WellKnownTableNames.EventSequenceMutationHeads);

        return (
            heads.Columns.Single(_ => _.Name == "LastAssignedOrdinal").ColumnType!,
            heads.Columns.Single(_ => _.Name == "ActiveCommandPayload").ColumnType!,
            heads.Columns.Single(_ => _.Name == "ActiveMutationId").ColumnType!);
    }

    static IReadOnlyDictionary<string, string> DigestColumnTypesFor(string provider)
    {
        var migrationBuilder = new MigrationBuilder(provider);
        new ExposedMigration().Apply(migrationBuilder);
        var operations = migrationBuilder.Operations.OfType<CreateTableOperation>();
        var heads = operations.Single(_ => _.Name == WellKnownTableNames.EventSequenceMutationHeads);
        var history = operations.Single(_ => _.Name == WellKnownTableNames.EventSequenceMutationHistory);

        return new Dictionary<string, string>
        {
            ["ActiveDefinitionDigestV1"] = heads.Columns.Single(_ => _.Name == "ActiveDefinitionDigestV1").ColumnType!,
            ["DefinitionDigestV1"] = history.Columns.Single(_ => _.Name == "DefinitionDigestV1").ColumnType!,
            ["ReceiptDigestV1"] = history.Columns.Single(_ => _.Name == "ReceiptDigestV1").ColumnType!
        };
    }

    sealed class ExposedMigration : MutationMigration
    {
        public void Apply(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
