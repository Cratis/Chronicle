// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reactors.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReactorDefinitions}-{nameof(v15_1_0)}")]
public class v15_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"ALTER TABLE \"{WellKnownTableNames.ReactorDefinitions}\" ADD COLUMN \"Tags\" TEXT");
        migrationBuilder.Sql($"ALTER TABLE \"{WellKnownTableNames.ReactorDefinitions}\" ADD COLUMN \"Filters\" TEXT");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
