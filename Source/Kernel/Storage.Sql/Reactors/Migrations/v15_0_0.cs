// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sql.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Reactors.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ReactorDefinitionsDbContext))]
[Migration($"{WellKnownTableNames.ReactorDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReactorDefinitions,
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                EventSequenceId = table.Column<bool>(type: "TEXT", nullable: false),
                EventTypes = table.JsonColumn<IEnumerable<EventTypeWithKeyExpression>>(migrationBuilder),
                IsReplayable = table.Column<int>(type: "INTEGER", nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReactorDefinitions}", x => x.Id));

        migrationBuilder.EnsureJsonColumn(WellKnownTableNames.ReactorDefinitions, "EventTypes");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.ReactorDefinitions);
    }
}
