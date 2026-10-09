// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.EventTypes}-{nameof(v19_36_0)}")]
public class v19_36_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing rows get visibility 0 (Unspecified) and no origin, which is exactly what clients that predate visibility send.
        migrationBuilder.AddColumn<int>(
            name: "Visibility",
            table: WellKnownTableNames.EventTypes,
            nullable: false,
            defaultValue: 0);
        migrationBuilder.AddColumn<string>(
            name: "Origin",
            table: WellKnownTableNames.EventTypes,
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Visibility", table: WellKnownTableNames.EventTypes);
        migrationBuilder.DropColumn(name: "Origin", table: WellKnownTableNames.EventTypes);
    }
}
