// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.Migrations;

/// <summary>
/// Adds an indexed occurrence identity to namespace effect receipts.
/// No production receipts exist yet: the preceding release shipped storage only,
/// and no receipt writer existed before occurrence delivery. No backfill is required.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration("NS-Occurrences-v19_68_0")]
public class v19_68_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddStringColumn("OccurrenceId", WellKnownTableNames.EffectReceipts, maxLength: 64, nullable: false, defaultValue: string.Empty);
        migrationBuilder.CreateIndex("IX_EffectReceipts_OccurrenceId", WellKnownTableNames.EffectReceipts, "OccurrenceId");
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_EffectReceipts_OccurrenceId", WellKnownTableNames.EffectReceipts);
        migrationBuilder.DropColumn("OccurrenceId", WellKnownTableNames.EffectReceipts);
    }
}
