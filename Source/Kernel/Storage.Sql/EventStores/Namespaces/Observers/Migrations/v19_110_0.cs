// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds the observer-level and per-event-type handled counts that the SQL providers dropped (#5299).
/// Existing rows read back as zero.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_110_0)}")]
public class v19_110_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddNumberColumn<ulong>(name: "HandledEventCount", table: WellKnownTableNames.Observers, nullable: false, defaultValue: 0UL);
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Native jsonb validates the value itself. Arc's AddJsonColumn generator joins
            // its auxiliary CHECK and CREATE INDEX without a statement separator.
            migrationBuilder.AddColumn<string>(name: "HandledEventCountPerEventType", table: WellKnownTableNames.Observers, type: "jsonb", nullable: true);
        }
        else
        {
            migrationBuilder.AddJsonColumn<IDictionary<string, ulong>>(name: "HandledEventCountPerEventType", table: WellKnownTableNames.Observers);
            migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == "HandledEventCountPerEventType").IsNullable = true;
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Observers_HandledEventCountPerEventType_IsJson", table: WellKnownTableNames.Observers);
        }

        migrationBuilder.DropColumn(name: "HandledEventCount", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "HandledEventCountPerEventType", table: WellKnownTableNames.Observers);
    }
}
