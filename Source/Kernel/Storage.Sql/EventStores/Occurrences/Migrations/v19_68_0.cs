// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.Migrations;

/// <summary>
/// Adds event-store trigger payload contracts after occurrence infrastructure.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-Occurrences-v19_68_0")]
public class v19_68_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "TriggerDefinitions",
        columns: table => new
        {
            Id = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
            Trigger = table.StringColumn(migrationBuilder),
            Name = table.StringColumn(migrationBuilder),
            Schema = table.StringColumn(migrationBuilder),
            Revision = table.NumberColumn<long>(migrationBuilder)
        },
        constraints: table => table.PrimaryKey("PK_TriggerDefinitions", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("TriggerDefinitions");
}
