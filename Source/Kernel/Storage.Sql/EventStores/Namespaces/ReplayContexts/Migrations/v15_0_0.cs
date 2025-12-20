// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ReplayContexts}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReplayContexts,
            columns: table => new
            {
                ReadModelIdentifier = table.StringColumn(migrationBuilder),
                ReadModel = table.StringColumn(migrationBuilder),
                RevertModel = table.StringColumn(migrationBuilder),
                Started = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReplayContexts}", x => x.ReadModelIdentifier));

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.ReplayContexts}_ReadModel",
            table: WellKnownTableNames.ReplayContexts,
            column: "ReadModel");
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.ReplayContexts);
    }
}
