// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.Migrations;

/// <summary>
/// Adds event-store observer registrations and independently versioned owner and floor records.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration($"{WellKnownTableNames.ObserverRegistrations}-{nameof(v19_113_0)}")]
public class v19_113_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ObserverRegistrations,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 80, nullable: false),
                Kind = table.StringColumn(migrationBuilder, maxLength: 16, nullable: false),
                Owner = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                IsActive = table.BoolColumn(migrationBuilder),
                Revision = table.StringColumn(migrationBuilder, maxLength: 20, nullable: false),
                Generation = table.StringColumn(migrationBuilder, maxLength: 20, nullable: false),
                Data = table.StringColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ObserverRegistrations", row => row.Id));
        migrationBuilder.CreateIndex("IX_ObserverRegistrations_Owner", WellKnownTableNames.ObserverRegistrations, ["Kind", "Owner"]);
        migrationBuilder.CreateIndex("IX_ObserverRegistrations_IsActive", WellKnownTableNames.ObserverRegistrations, ["Kind", "IsActive"]);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.ObserverRegistrations);
}
