// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Migrations;

/// <summary>
/// Adds a completion marker independent of implicit event store creation.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-Initialization-v19_70_0")]
public class v19_70_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "EventStoreInitialization",
        columns: table => new
        {
            Id = table.NumberColumn<int>(migrationBuilder, nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_EventStoreInitialization", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("EventStoreInitialization");
}
