// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Imports}-{nameof(v19_143_0)}")]
public class v19_143_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Imports,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                State = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Payload = table.JsonColumn<string>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Imports}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ImportChunks,
            columns: table => new
            {
                ImportId = table.GuidColumn(migrationBuilder, nullable: false),
                ChunkIndex = table.NumberColumn<int>(migrationBuilder, nullable: false),
                FirstEventIndex = table.NumberColumn<long>(migrationBuilder, nullable: false),
                EventsReceived = table.NumberColumn<int>(migrationBuilder, nullable: false),
                EventsStaged = table.NumberColumn<int>(migrationBuilder, nullable: false),
                EventsRejected = table.NumberColumn<int>(migrationBuilder, nullable: false),
                EventTypes = table.JsonColumn<string>(migrationBuilder),
                Payload = table.JsonColumn<string>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ImportChunks}", x => new { x.ImportId, x.ChunkIndex }));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ImportChunks);
        migrationBuilder.DropTable(name: WellKnownTableNames.Imports);
    }
}
