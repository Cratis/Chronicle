// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ReplayedReadModels}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReplayedReadModels,
            columns: table => new
            {
                ObserverId = table.StringColumn(migrationBuilder),
                ReadModelIdentifier = table.StringColumn(migrationBuilder),
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

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ReplayedReadModels);
    }
}
