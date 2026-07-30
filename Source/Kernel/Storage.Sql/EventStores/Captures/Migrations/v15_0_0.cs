// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.Captures}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Captures,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Name = table.StringColumn(migrationBuilder),
                Declaration = table.StringColumn(migrationBuilder),
                Status = table.NumberColumn<int>(migrationBuilder, nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Captures}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.CaptureObservations,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Items = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.CaptureObservations}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.CaptureObservations);

        migrationBuilder.DropTable(
            name: WellKnownTableNames.Captures);
    }
}
