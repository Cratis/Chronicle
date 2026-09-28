// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Recreates the replayed read models table so it can hold every replay of a read model. The table
/// created by <see cref="v15_0_0"/> was never written to - the entity was mapped to a table name no
/// migration created - so it is always empty and can be dropped. Its key allowed one row per observer,
/// which made every replay after the first one fail, and it had no read model generation.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ReplayedReadModels}-{nameof(v19_14_0)}")]
public class v19_14_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ReplayedReadModels);

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReplayedReadModels,
            columns: table => new
            {
                ObserverId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Started = table.Column<DateTimeOffset>(nullable: false),
                ReadModelIdentifier = table.StringColumn(migrationBuilder, maxLength: 200),
                Generation = table.NumberColumn<uint>(migrationBuilder, nullable: false, defaultValue: ReadModelGeneration.FirstValue),
                ReadModelName = table.StringColumn(migrationBuilder),
                RevertModelName = table.StringColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReplayedReadModels}", x => new { x.ObserverId, x.Started }));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.ReplayedReadModels}_ReadModelIdentifier",
            table: WellKnownTableNames.ReplayedReadModels,
            column: "ReadModelIdentifier");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ReplayedReadModels);

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReplayedReadModels,
            columns: table => new
            {
                ObserverId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                ReadModelIdentifier = table.StringColumn(migrationBuilder, maxLength: 200),
                ReadModelName = table.StringColumn(migrationBuilder),
                RevertModelName = table.StringColumn(migrationBuilder),
                Started = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReplayedReadModels}", x => x.ObserverId));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.ReplayedReadModels}_ReadModelIdentifier",
            table: WellKnownTableNames.ReplayedReadModels,
            column: "ReadModelIdentifier");
    }
}
