// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Creates only v2 metadata; never changes an existing namespace or v1 table.
/// </summary>
static class V2MetadataMigration
{
    internal static Task CreateAuthority(ITableMigrator<V2DbContext> migrator, V2DbContext context, string table)
    {
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(table, columns => new
        {
            Id = columns.StringColumn(migration, maxLength: 64, nullable: false),
            Sequence = columns.StringColumn(migration, nullable: false),
            Constraint = columns.StringColumn(migration, nullable: false),
            Epoch = columns.GuidColumn(migration, nullable: false),
            IsLive = columns.BoolColumn(migration, nullable: false),
            RunId = columns.GuidColumn(migration, nullable: true),
            Revision = columns.NumberColumn<long>(migration, nullable: false)
        }, constraints: constraints => constraints.PrimaryKey($"PK_{table}", row => row.Id));
        ApplyCollation(context, migration, "Id");
        return migrator.ExecuteMigrationOperations(context, migration);
    }

    internal static Task CreateBindings(ITableMigrator<V2DbContext> migrator, V2DbContext context, string table)
    {
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.CreateTable(table, columns => new
        {
            Id = columns.StringColumn(migration, maxLength: 64, nullable: false),
            Sequence = columns.StringColumn(migration, nullable: false),
            Constraint = columns.StringColumn(migration, nullable: false),
            Mode = columns.NumberColumn<int>(migration, nullable: false),
            Table = columns.StringColumn(migration, maxLength: 63, nullable: false)
        }, constraints: constraints => constraints.PrimaryKey($"PK_{table}", row => row.Id));
        migration.CreateIndex("IX_UC2Bindings_Table", table, "Table", unique: true);
        ApplyCollation(context, migration, "Id", "Table");
        return migrator.ExecuteMigrationOperations(context, migration);
    }

    internal static void ApplyCollation(V2DbContext context, MigrationBuilder migration, params string[] names)
    {
        var collation = context.Database.IsSqlServer() ? V2Identity.BinaryCollation : context.Database.IsNpgsql() ? "C" : "BINARY";
        foreach (var column in migration.Operations.OfType<CreateTableOperation>().SelectMany(table => table.Columns).Where(column => names.Contains(column.Name)))
        {
            column.Collation = collation;
        }
    }
}
