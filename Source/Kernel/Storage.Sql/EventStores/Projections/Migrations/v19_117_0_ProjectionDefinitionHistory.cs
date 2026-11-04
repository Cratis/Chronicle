// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.Migrations;

/// <summary>
/// Installs inert projection definition history without modifying existing definitions.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-ProjectionDefinitionHistory-v19_117_0")]
public class v19_117_0_ProjectionDefinitionHistory : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ProjectionDefinitionHistory,
            columns: table => new
            {
                ProjectionId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Digest = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                DigestAlgorithm = table.StringColumn(migrationBuilder, nullable: false),
                RegisteringVersion = table.StringColumn(migrationBuilder, nullable: true),
                RecordedAt = table.DateTimeOffsetColumn(migrationBuilder),
                Definitions = table.JsonColumn<IDictionary<string, string>>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey("PK_ProjectionDefinitionHistory", row => new { row.ProjectionId, row.Digest }));
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.ProjectionDefinitionHistory);
}
