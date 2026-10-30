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
            name: "GenerationInitializationBlockers",
            columns: table => new
            {
                EventStore = table.StringColumn(migrationBuilder, nullable: false),
                Subject = table.StringColumn(migrationBuilder, maxLength: 400, nullable: false),
                Kind = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Detail = table.StringColumn(migrationBuilder, nullable: false),
                RaisedAt = table.DateTimeOffsetColumn(migrationBuilder),
                ResolvedAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_GenerationInitializationBlockers", row => new { row.Kind, row.Subject }));
        migrationBuilder.CreateTable(
            name: "GenerationInitializationReceipts",
            columns: table => new
            {
                EventStore = table.StringColumn(migrationBuilder, nullable: false),
                Subject = table.StringColumn(migrationBuilder, maxLength: 400, nullable: false),
                Step = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Evidence = table.StringColumn(migrationBuilder, nullable: false),
                Incarnation = table.GuidColumn(migrationBuilder, nullable: false),
                FloorRevision = table.NumberColumn<long>(migrationBuilder, nullable: false),
                RecordedAt = table.DateTimeOffsetColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey("PK_GenerationInitializationReceipts", row => new { row.Step, row.Subject }));
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
        migrationBuilder.DropTable("GenerationInitializationReceipts");
        migrationBuilder.DropTable("GenerationInitializationBlockers");
        migrationBuilder.DropTable("GenerationFloorHistory");
        migrationBuilder.DropTable("GenerationFloor");
    }
}
