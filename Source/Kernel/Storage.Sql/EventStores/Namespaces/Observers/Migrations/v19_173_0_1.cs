// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds the nullable JSON column that holds an observer's durable subscription, without backfilling legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_173_0_1)}")]
public class v19_173_0_1 : Migration
{
    const string Column = "DurableSubscription";

    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Native jsonb validates the value itself. Arc's AddJsonColumn generator joins
            // its auxiliary CHECK and CREATE INDEX without a statement separator.
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
