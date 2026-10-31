// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.Migrations;

/// <summary>
/// Adds durable lifecycle namespace snapshots at event-store scope.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-Occurrences-v19_77_0")]
public class v19_77_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "LifecycleFanOutIntents",
        columns: table => new
        {
            Id = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
            Data = table.StringColumn(migrationBuilder, nullable: false),
            Count = table.NumberColumn<int>(migrationBuilder, nullable: false),
            Progress = table.NumberColumn<int>(migrationBuilder, nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_LifecycleFanOutIntents", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("LifecycleFanOutIntents");
}
