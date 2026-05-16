// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReducerDefinitions}-{nameof(v15_1_0)}")]
public class v15_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Tags",
            table: WellKnownTableNames.ReducerDefinitions,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Filters",
            table: WellKnownTableNames.ReducerDefinitions,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
