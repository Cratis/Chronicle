// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ObserverHandledCounts}-{nameof(v19_92_0)}")]
public class v19_92_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ObserverHandledCounts,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                ObserverId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Partition = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                EventTypeId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Count = table.NumberColumn<long>(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ObserverHandledCounts}", x => x.Id));
        migrationBuilder.CreateIndex(
            $"IX_{WellKnownTableNames.ObserverHandledCounts}_ObserverId_Partition_EventTypeId",
            WellKnownTableNames.ObserverHandledCounts,
            ["ObserverId", "Partition", "EventTypeId"],
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: WellKnownTableNames.ObserverHandledCounts);
}
