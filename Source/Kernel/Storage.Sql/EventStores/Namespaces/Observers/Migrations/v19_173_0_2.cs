// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds optional durable catch-up repair obligations without backfilling legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_173_0_2)}")]
public class v19_173_0_2 : Migration
{
    const string Column = "PendingCatchUpRepairs";

    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.AddColumn<string>(name: Column, table: WellKnownTableNames.Observers, type: "jsonb", nullable: true);
            return;
        }

        migrationBuilder.AddJsonColumn<string>(name: Column, table: WellKnownTableNames.Observers);
        migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == Column).IsNullable = true;
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.DropCheckConstraint(name: $"CK_Observers_{Column}_IsJson", table: WellKnownTableNames.Observers);
        }

        migrationBuilder.DropColumn(name: Column, table: WellKnownTableNames.Observers);
    }
}
