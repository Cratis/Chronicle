// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReducerDefinitions}-{nameof(v18_2_0)}")]
public class v18_2_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Hash",
            table: WellKnownTableNames.ReducerDefinitions,
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);
        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: WellKnownTableNames.ReducerDefinitions,
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Hash", table: WellKnownTableNames.ReducerDefinitions);
        migrationBuilder.DropColumn(name: "IsActive", table: WellKnownTableNames.ReducerDefinitions);
    }
}
