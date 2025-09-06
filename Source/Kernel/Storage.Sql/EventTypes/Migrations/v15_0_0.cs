// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventTypes.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventTypesDbContext))]
[Migration($"{WellKnownTableNames.EventTypes}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventTypes,
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                Tombstone = table.Column<bool>(type: "INTEGER", nullable: false),
                Schemas = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventTypes}", x => x.Id));

        migrationBuilder.EnsureJsonColumn(WellKnownTableNames.EventTypes, "Schemas");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.EventTypes);
    }
}
