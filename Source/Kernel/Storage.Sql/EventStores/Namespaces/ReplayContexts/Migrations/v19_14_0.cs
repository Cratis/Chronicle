// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds the read model generation a replay context was established for. The entity has carried
/// the property since read model generations were introduced, but no migration created the column,
/// so establishing a replay context failed on every SQL provider.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ReplayContexts}-{nameof(v19_14_0)}")]
public class v19_14_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddNumberColumn<uint>(
            name: nameof(ReplayContextEntry.Generation),
            table: WellKnownTableNames.ReplayContexts,
            nullable: false,
            defaultValue: ReadModelGeneration.FirstValue);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: nameof(ReplayContextEntry.Generation), table: WellKnownTableNames.ReplayContexts);
    }
}
