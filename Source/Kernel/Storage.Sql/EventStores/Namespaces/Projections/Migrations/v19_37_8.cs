// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ProjectionFutures}-{nameof(v19_37_8)}")]
public class v19_37_8 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Store pre-serialized JSON as text so every provider uses the same string mapping.
        // Null preserves the legacy context fallback for existing futures.
        migrationBuilder.AddColumn<string>(
            name: nameof(ProjectionFutureEntity.EventContextJson),
            table: WellKnownTableNames.ProjectionFutures,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: nameof(ProjectionFutureEntity.EventContextJson),
            table: WellKnownTableNames.ProjectionFutures);
    }
}
