// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ProjectionFutures}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ProjectionFutures,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                ProjectionId = table.StringColumn(migrationBuilder),
                EventSequenceNumber = table.NumberColumn<ulong>(migrationBuilder),
                EventTypeId = table.StringColumn(migrationBuilder),
                EventTypeGeneration = table.NumberColumn<uint>(migrationBuilder),
                EventSourceId = table.StringColumn(migrationBuilder),
                EventContentJson = table.JsonColumn<string>(migrationBuilder),
                ParentPath = table.StringColumn(migrationBuilder),
                ChildPath = table.StringColumn(migrationBuilder),
                IdentifiedByProperty = table.StringColumn(migrationBuilder),
                ParentIdentifiedByProperty = table.StringColumn(migrationBuilder),
                ParentKeyJson = table.JsonColumn<string>(migrationBuilder),
                Created = table.DateTimeOffsetColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ProjectionFutures}", x => x.Id));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.ProjectionFutures}_ProjectionId",
            table: WellKnownTableNames.ProjectionFutures,
            column: "ProjectionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ProjectionFutures);
    }
}
