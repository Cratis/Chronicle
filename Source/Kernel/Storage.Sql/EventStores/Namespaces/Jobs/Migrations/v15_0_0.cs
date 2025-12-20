// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Jobs.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Jobs}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Jobs,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                Type = table.StringColumn(migrationBuilder),
                Status = table.NumberColumn<int>(migrationBuilder),
                Created = table.Column<DateTimeOffset>(nullable: false),
                StateJson = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Jobs}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Jobs}_Status",
            table: WellKnownTableNames.Jobs,
            column: "Status");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Jobs}_Type",
            table: WellKnownTableNames.Jobs,
            column: "Type");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.Jobs);
    }
}
