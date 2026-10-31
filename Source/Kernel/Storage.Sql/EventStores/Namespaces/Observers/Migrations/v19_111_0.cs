// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds the nullable time of the observer's last running state change without backfilling legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_111_0)}")]
public class v19_111_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddDateTimeOffsetColumn("RunningStateChangedAt", WellKnownTableNames.Observers, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("RunningStateChangedAt", WellKnownTableNames.Observers);
}
