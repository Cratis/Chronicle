// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Patterns.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.BehaviorPatterns}-{nameof(v16_39_0)}")]
public class v16_39_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.BehaviorPatterns,
            columns: table => new
            {
                GroupingKey = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                FacetSetHash = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                FacetSetKey = table.StringColumn(migrationBuilder),
                FacetsJson = table.JsonColumn<string>(migrationBuilder),
                Occurrences = table.NumberColumn<long>(migrationBuilder, nullable: false),
                Confidence = table.NumberColumn<double>(migrationBuilder, nullable: false),
                Support = table.NumberColumn<double>(migrationBuilder, nullable: false),
                Weight = table.NumberColumn<double>(migrationBuilder, nullable: false),
                FirstSeen = table.Column<DateTimeOffset>(nullable: false),
                LastSeen = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey(
                $"PK_{WellKnownTableNames.BehaviorPatterns}",
                x => new { x.GroupingKey, x.FacetSetHash }));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.BehaviorPatterns}_LastSeen",
            table: WellKnownTableNames.BehaviorPatterns,
            column: "LastSeen");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.BehaviorPatterns);
    }
}
