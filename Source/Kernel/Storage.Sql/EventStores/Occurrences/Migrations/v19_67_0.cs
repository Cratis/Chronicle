// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.Migrations;

/// <summary>
/// Adds application-scoped occurrences and schedule definitions without touching event tables.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-Occurrences-v19_67_0")]
public class v19_67_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => OccurrenceTablesMigration.Up(migrationBuilder);

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => OccurrenceTablesMigration.Down(migrationBuilder);
}
