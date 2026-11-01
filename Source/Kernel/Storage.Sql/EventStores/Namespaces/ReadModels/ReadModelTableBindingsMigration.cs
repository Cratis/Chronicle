// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Bootstraps only the ownership ledger, never the database or a dynamic user table.
/// </summary>
/// <param name="migrator">The existing dynamic-table migration executor.</param>
internal sealed class ReadModelTableBindingsMigration(ITableMigrator<ReadModelTableBindingsDbContext> migrator)
{
    /// <summary>
    /// Creates the ledger when absent and validates its shape.
    /// </summary>
    /// <param name="context">The metadata-only ledger context.</param>
    /// <param name="schema">The resolved schema.</param>
    /// <returns>A task that completes when the ledger is usable.</returns>
    internal async Task Ensure(ReadModelTableBindingsDbContext context, string schema)
    {
        var catalog = new ReadModelTableCatalog(context, schema);
        if (await catalog.Find(WellKnownTableNames.ReadModelTableNames) is null)
        {
            await Create(context, WellKnownTableNames.ReadModelTableNames);
        }
        await ReadModelTableBindingsSchema.Validate(context, schema);
    }

    static ColumnTypes TypesFor(DatabaseType provider) => provider switch
    {
        DatabaseType.PostgreSql => new("text", "character varying(64)", "character varying(128)", "integer", "C", "C"),
        DatabaseType.SqlServer => new("nvarchar(max)", "nvarchar(64)", "nvarchar(128)", "int", "Latin1_General_100_BIN2", null),
        _ => new("text", "TEXT", "TEXT", "INTEGER", "BINARY", "NOCASE")
    };

    Task Create(ReadModelTableBindingsDbContext context, string tableName)
    {
        var types = TypesFor(context.Database.GetDatabaseType());
        var builder = new MigrationBuilder(context.Database.ProviderName);
        builder.CreateTable(
            name: tableName,
            columns: table => new
            {
                Id = table.Column<string>(type: types.Key, maxLength: 64, nullable: false, collation: types.IdCollation),
                EventStoreId = table.Column<string>(type: types.Text, nullable: false),
                Namespace = table.Column<string>(type: types.Text, nullable: false),
                ReadModelIdentifier = table.Column<string>(type: types.Text, nullable: false),
                ContainerName = table.Column<string>(type: types.Text, nullable: false),
                Role = table.Column<int>(type: types.Integer, nullable: false),
                LogicalName = table.Column<string>(type: types.Text, nullable: false),
                PhysicalName = table.Column<string>(type: types.Name, maxLength: 128, nullable: false, collation: types.NameCollation),
                State = table.Column<int>(type: types.Integer, nullable: false),
                ReplayAttempt = table.Column<string>(type: types.Text, nullable: true)
            },
            schema: context.Schema,
            constraints: table => table.PrimaryKey("PK_chronicle_read_model_table_names", binding => binding.Id));
        builder.CreateIndex(
            name: "IX_chronicle_read_model_table_names_PhysicalName",
            table: tableName,
            column: "PhysicalName",
            schema: context.Schema,
            unique: true);
        return migrator.ExecuteMigrationOperations(context, builder);
    }

    sealed record ColumnTypes(string Text, string Key, string Name, string Integer, string IdCollation, string? NameCollation);
}
