// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds nullable durable partition replay completions without changing legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_111_0_0)}")]
public class v19_111_0_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.AddColumn<string>(name: "PartitionReplayCompletions", table: WellKnownTableNames.Observers, type: "jsonb", nullable: true);
        }
        else
        {
            migrationBuilder.AddJsonColumn<string>(name: "PartitionReplayCompletions", table: WellKnownTableNames.Observers);
            migrationBuilder.Operations.OfType<AddColumnOperation>().Single().IsNullable = true;
        }
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            migrationBuilder.DropCheckConstraint(name: "CK_Observers_PartitionReplayCompletions_IsJson", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "PartitionReplayCompletions", table: WellKnownTableNames.Observers);
    }
}
