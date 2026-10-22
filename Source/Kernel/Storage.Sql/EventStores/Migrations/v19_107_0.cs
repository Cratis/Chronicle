// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Migrations;

/// <summary>Installs additive generation floor and audit tables without writing generation rows.</summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-Generations-v19_107_0")]
public class v19_107_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GenerationFloor",
            columns: table => new
            {
                Id = table.NumberColumn<int>(migrationBuilder, nullable: false),
                MinimumStorageProtocol = table.NumberColumn<int>(migrationBuilder, nullable: false),
                MinimumWriterProtocol = table.NumberColumn<int>(migrationBuilder, nullable: false),
                RequiredCapabilities = table.StringColumn(migrationBuilder, nullable: false),
                Incarnation = table.GuidColumn(migrationBuilder, nullable: false),
                Revision = table.NumberColumn<long>(migrationBuilder, nullable: false),
                AdvancedAt = table.DateTimeOffsetColumn(migrationBuilder),
                AdvancedBy = table.StringColumn(migrationBuilder, nullable: false),
                Reason = table.StringColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_GenerationFloor", row => row.Id));
        migrationBuilder.CreateTable(
            name: "GenerationFloorHistory",
            columns: table => new
            {
                MinimumStorageProtocol = table.NumberColumn<int>(migrationBuilder, nullable: false),
                MinimumWriterProtocol = table.NumberColumn<int>(migrationBuilder, nullable: false),
                RequiredCapabilities = table.StringColumn(migrationBuilder, nullable: false),
                Incarnation = table.GuidColumn(migrationBuilder, nullable: false),
                Revision = table.NumberColumn<long>(migrationBuilder, nullable: false),
                AdvancedAt = table.DateTimeOffsetColumn(migrationBuilder),
                AdvancedBy = table.StringColumn(migrationBuilder, nullable: false),
                Reason = table.StringColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_GenerationFloorHistory", row => row.Revision));
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GenerationFloorHistory");
        migrationBuilder.DropTable("GenerationFloor");
    }
}
