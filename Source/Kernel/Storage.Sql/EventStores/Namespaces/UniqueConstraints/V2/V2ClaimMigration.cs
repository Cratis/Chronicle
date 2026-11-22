// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Publishes the physical table and its logical binding in the same DDL transaction.
/// </summary>
static class V2ClaimMigration
{
    /// <summary>
    /// Creates a claim table and publishes its binding atomically.
    /// </summary>
    /// <param name="migrator">The table migrator.</param>
    /// <param name="context">The schema context.</param>
    /// <param name="binding">The logical and physical binding.</param>
    /// <returns>The migration operation.</returns>
    internal static Task Create(ITableMigrator<V2DbContext> migrator, V2DbContext context, V2Binding binding)
    {
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(
            binding.Table,
            columns => new
            {
                Scope = columns.StringColumn(migration, maxLength: 32, nullable: false),
                EventSourceId = columns.StringColumn(migration, maxLength: V2Identity.OwnerLimit, nullable: false),
                Value = columns.StringColumn(migration, maxLength: 64, nullable: false),
                SequenceNumber = columns.NumberColumn<decimal>(migration, nullable: false),
                Subject = columns.StringColumn(migration, nullable: true)
            },
            constraints: constraints =>
            {
                if (binding.Mode == UniqueConstraintMode.PerValue) constraints.PrimaryKey($"PK_{binding.Table}", row => new { row.Scope, row.Value }).Annotation("SqlServer:Clustered", false);
                else constraints.PrimaryKey($"PK_{binding.Table}", row => new { row.Scope, row.EventSourceId }).Annotation("SqlServer:Clustered", false);
            });
        ConfigureTypes(context, migration);
        V2MetadataMigration.ApplyCollation(context, migration, "Scope", "Value", "EventSourceId");
        migration.CreateIndex($"IX_{binding.Table}_V", binding.Table, ["Scope", "Value"], unique: true);
        migration.CreateIndex($"IX_{binding.Table}_O", binding.Table, ["Scope", "EventSourceId"]);
        migration.InsertData(V2Identity.Bindings, ["Id", "Sequence", "Constraint", "Mode", "Table"], [binding.Id, binding.Sequence, binding.Constraint, (int)binding.Mode, binding.Table]);
        return migrator.ExecuteMigrationOperations(context, migration);
    }

    static void ConfigureTypes(V2DbContext context, MigrationBuilder migration)
    {
        var columns = migration.Operations.OfType<CreateTableOperation>().Single().Columns;
        foreach (var column in columns.Where(column => string.Equals(column.Name, "Scope", StringComparison.Ordinal) || string.Equals(column.Name, "Value", StringComparison.Ordinal)))
        {
            column.ColumnType = context.Database.IsSqlite() ? "TEXT" : $"char({column.MaxLength})";
            column.IsUnicode = false;
            column.IsFixedLength = true;
        }

        var sequence = columns.Single(column => column.Name == "SequenceNumber");
        sequence.ColumnType = context.Database.IsSqlite() ? "TEXT" : "decimal(20,0)";
        sequence.Precision = 20;
        sequence.Scale = 0;
    }
}
