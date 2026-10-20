// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.InFlightEvents}-{nameof(v19_93_0)}")]
public class v19_93_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.InFlightEvents,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                ObserverId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Partition = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                EventSequenceNumber = table.Column<decimal>(type: ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite" ? "TEXT" : "decimal(20,0)", nullable: false, precision: 20, scale: 0)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.InFlightEvents}", x => x.Id));
        migrationBuilder.CreateIndex(
            $"IX_{WellKnownTableNames.InFlightEvents}_ObserverId_Partition_EventSequenceNumber",
            WellKnownTableNames.InFlightEvents,
            ["ObserverId", "Partition", "EventSequenceNumber"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: WellKnownTableNames.InFlightEvents);
}
