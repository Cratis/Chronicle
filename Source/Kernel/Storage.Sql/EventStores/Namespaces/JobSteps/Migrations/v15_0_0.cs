// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.JobSteps.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.JobSteps}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.JobSteps,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                JobId = table.GuidColumn(migrationBuilder),
                JobStepId = table.GuidColumn(migrationBuilder),
                Type = table.StringColumn(migrationBuilder),
                Name = table.StringColumn(migrationBuilder),
                Status = table.NumberColumn<int>(migrationBuilder),
                IsPrepared = table.BoolColumn(migrationBuilder),
                StateJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.JobSteps}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_JobId",
            table: WellKnownTableNames.JobSteps,
            column: "JobId");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_Status",
            table: WellKnownTableNames.JobSteps,
            column: "Status");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.JobSteps}_Type",
            table: WellKnownTableNames.JobSteps,
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.JobSteps);
    }
}
