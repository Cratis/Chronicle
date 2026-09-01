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

    void Because()
    {
        _types = new Dictionary<string, (string, string, string)>
        {
            [PostgreSql] = ColumnTypesFor(PostgreSql),
            [SqlServer] = ColumnTypesFor(SqlServer),
            [Sqlite] = ColumnTypesFor(Sqlite)
        };
    }

    [Fact] void should_use_postgresql_native_types() => _types[PostgreSql].ShouldEqual(("BIGINT", "TEXT", "UUID"));
    [Fact] void should_use_sql_server_native_types() => _types[SqlServer].ShouldEqual(("BIGINT", "NVARCHAR(MAX)", "UNIQUEIDENTIFIER"));
    [Fact] void should_use_sqlite_canonical_text_mutation_ids() => _types[Sqlite].ShouldEqual(("INTEGER", "TEXT", "TEXT"));

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

    sealed class ExposedMigration : MutationMigration
    {
        public void Apply(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
