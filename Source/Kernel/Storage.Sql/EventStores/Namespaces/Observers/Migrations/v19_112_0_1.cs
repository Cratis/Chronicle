// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds nullable shadow ownership metadata without backfilling legacy observers.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_112_0_1)}")]
public class v19_112_0_1 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddNullableJsonColumn(migrationBuilder, "PartitionClaims");
        AddNullableJsonColumn(migrationBuilder, "ObserverWideClaim");
        migrationBuilder.AddNumberColumn<ulong>(name: "OwnershipRevision", table: WellKnownTableNames.Observers, nullable: true);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Observers_PartitionClaims_IsJson", table: WellKnownTableNames.Observers);
            migrationBuilder.DropCheckConstraint(name: "CK_Observers_ObserverWideClaim_IsJson", table: WellKnownTableNames.Observers);
        }

        migrationBuilder.DropColumn(name: "PartitionClaims", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "ObserverWideClaim", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "OwnershipRevision", table: WellKnownTableNames.Observers);
    }

    void AddNullableJsonColumn(MigrationBuilder migrationBuilder, string name)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Arc's auxiliary JSON check/index SQL lacks a separator; jsonb validates the value itself.
            migrationBuilder.AddColumn<string>(name: name, table: WellKnownTableNames.Observers, type: "jsonb", nullable: true);
            return;
        }

        migrationBuilder.AddJsonColumn<string>(name: name, table: WellKnownTableNames.Observers);
        migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == name).IsNullable = true;
    }
}
