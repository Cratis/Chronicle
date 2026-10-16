// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds nullable constraint index coverage and catch-up state without inventing coverage for existing sequences.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequences}-{nameof(v19_74_3)}")]
public class v19_74_3 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var numberType = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite" ? "TEXT" : "decimal(20,0)";
        migrationBuilder.AddColumn<decimal>(name: "ConstraintIndexWatermark", table: WellKnownTableNames.EventSequences, type: numberType, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ConstraintIndexCatchUpBelow", table: WellKnownTableNames.EventSequences, type: numberType, nullable: true);
        migrationBuilder.AddStringColumn("ConstraintIndexCatchUpRefusing", WellKnownTableNames.EventSequences, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConstraintIndexWatermark", table: WellKnownTableNames.EventSequences);
        migrationBuilder.DropColumn(name: "ConstraintIndexCatchUpBelow", table: WellKnownTableNames.EventSequences);
        migrationBuilder.DropColumn(name: "ConstraintIndexCatchUpRefusing", table: WellKnownTableNames.EventSequences);
    }
}
