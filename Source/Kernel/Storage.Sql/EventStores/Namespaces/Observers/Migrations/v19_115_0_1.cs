// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds the nullable time of the observer's last running state change without backfilling legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_115_0_1)}")]
public class v19_115_0_1 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddDateTimeOffsetColumn("RunningStateChangedAt", WellKnownTableNames.Observers, nullable: true);

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("RunningStateChangedAt", WellKnownTableNames.Observers);
}
