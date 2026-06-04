// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ClosedStreams}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ClosedStreams,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder, maxLength: 255, nullable: false),
                StreamType = table.StringColumn(migrationBuilder, maxLength: 255, nullable: false),
                StreamId = table.StringColumn(migrationBuilder, maxLength: 255, nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", x => new { x.EventSequenceId, x.StreamType, x.StreamId }));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ClosedStreams);
    }
}
