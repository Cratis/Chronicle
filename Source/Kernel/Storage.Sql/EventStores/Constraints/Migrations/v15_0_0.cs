// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.Constraints}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Constraints,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 255, nullable: false),
                Name = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Version = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                Type = table.StringColumn(migrationBuilder),
                Definition = table.JsonColumn<string>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Constraints}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.Constraints}_Name_Version",
            table: WellKnownTableNames.Constraints,
            columns: ["Name", "Version"],
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.Constraints);
    }
}
