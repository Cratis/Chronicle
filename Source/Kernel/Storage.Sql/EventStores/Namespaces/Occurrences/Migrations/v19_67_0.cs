// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.Migrations;

/// <summary>
/// Adds namespace-scoped occurrences, fenced cursors and shared effect receipts.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration("NS-Occurrences-v19_67_0")]
public class v19_67_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        OccurrenceTablesMigration.Up(migrationBuilder);
        OccurrenceTablesMigration.UpReceipts(migrationBuilder);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(WellKnownTableNames.EffectReceipts);
        OccurrenceTablesMigration.Down(migrationBuilder);
    }
}
