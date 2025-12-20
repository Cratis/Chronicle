// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.FailedPartitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.FailedPartitions,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                Partition = table.StringColumn(migrationBuilder),
                ObserverId = table.StringColumn(migrationBuilder),
                IsResolved = table.BoolColumn(migrationBuilder),
                StateJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.FailedPartitions}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.FailedPartitions}_IsResolved",
            table: WellKnownTableNames.FailedPartitions,
            column: "IsResolved");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.FailedPartitions}_ObserverId",
            table: WellKnownTableNames.FailedPartitions,
            column: "ObserverId");

        migrationBuilder.CreateIndex(
            name: "IX_FailedPartitions_Partition",
            table: "FailedPartitions",
            column: "Partition");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.FailedPartitions);
    }
}
