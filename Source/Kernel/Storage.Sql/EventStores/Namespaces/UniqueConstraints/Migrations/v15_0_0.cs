// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.UniqueConstraintIndexes}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.UniqueConstraintIndexes,
            columns: table => new
            {
                EventSourceId = table.StringColumn(migrationBuilder),
                ConstraintName = table.StringColumn(migrationBuilder),
                Value = table.StringColumn(migrationBuilder),
                SequenceNumber = table.Column<decimal>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.UniqueConstraintIndexes}", x => new { x.EventSourceId, x.ConstraintName }));

        // Create indexes for constraint validation queries
        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.UniqueConstraintIndexes}_ConstraintName_Value",
            table: WellKnownTableNames.UniqueConstraintIndexes,
            columns: ["ConstraintName", "Value"]);

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.UniqueConstraintIndexes}_EventSourceId",
            table: WellKnownTableNames.UniqueConstraintIndexes,
            column: "EventSourceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.UniqueConstraintIndexes);
    }
}
