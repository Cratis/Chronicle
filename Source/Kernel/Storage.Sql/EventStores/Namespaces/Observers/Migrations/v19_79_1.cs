// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds empty, forward-only definition-change row storage. No legacy observer is initialized or backfilled.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_79_1)}")]
public class v19_79_1 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ObserverDefinitionChangeRows,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                Observer = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                Family = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                Revision = table.NumberColumn<long>(migrationBuilder, nullable: false),
                Version = table.NumberColumn<long>(migrationBuilder, nullable: false),
                State = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Position = table.Column<decimal>(type: "decimal(20,0)", nullable: true, precision: 20, scale: 0),
                Data = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL"
                    ? table.Column<string>(type: "jsonb", nullable: false)
                    : table.JsonColumn<string>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey("PK_ObserverDefinitionChangeRows", row => row.Id));
        migrationBuilder.CreateIndex("IX_ObserverDefinitionChangeRows_Observer_Family_Revision_Id", WellKnownTableNames.ObserverDefinitionChangeRows, ["Observer", "Family", "Revision", "Id"]);
        migrationBuilder.CreateIndex("IX_ObserverDefinitionChangeRows_Observer_Family_Id", WellKnownTableNames.ObserverDefinitionChangeRows, ["Observer", "Family", "Id"]);
    }
}
