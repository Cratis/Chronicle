// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Recommendations}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Recommendations,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                Name = table.StringColumn(migrationBuilder),
                Description = table.StringColumn(migrationBuilder),
                Type = table.StringColumn(migrationBuilder),
                Occurred = table.Column<DateTimeOffset>(nullable: false),
                RequestJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Recommendations}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Recommendations}_Type",
            table: WellKnownTableNames.Recommendations,
            column: "Type");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Recommendations}_Occurred",
            table: WellKnownTableNames.Recommendations,
            column: "Occurred");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.Recommendations);
    }
}
