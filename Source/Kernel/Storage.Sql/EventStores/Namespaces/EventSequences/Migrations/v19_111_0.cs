// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds the nullable mutation epoch that every revision and redaction advances, without inventing one for existing sequences.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequences}-{nameof(v19_111_0)}")]
public class v19_111_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var numberType = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite" ? "TEXT" : "decimal(20,0)";
        migrationBuilder.AddColumn<decimal>(name: "MutationEpoch", table: WellKnownTableNames.EventSequences, type: numberType, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MutationEpoch", table: WellKnownTableNames.EventSequences);
    }
}
