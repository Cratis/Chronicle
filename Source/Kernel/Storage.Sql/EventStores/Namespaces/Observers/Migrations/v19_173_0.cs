// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

/// <summary>
/// Adds the nullable time a client was last seen holding a subscription to the observer, without backfilling legacy rows.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_173_0)}")]
public class v19_173_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddDateTimeOffsetColumn("LastSeenAt", WellKnownTableNames.Observers, nullable: true);

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("LastSeenAt", WellKnownTableNames.Observers);
}
