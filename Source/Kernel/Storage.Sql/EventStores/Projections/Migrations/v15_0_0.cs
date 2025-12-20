// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ProjectionDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ProjectionDefinitions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                Owner = table.NumberColumn<int>(migrationBuilder),
                ReadModelName = table.StringColumn(migrationBuilder),
                ReadModelGeneration = table.NumberColumn<int>(migrationBuilder),
                SinkType = table.GuidColumn(migrationBuilder),
                SinkConfigurationId = table.GuidColumn(migrationBuilder),
                Definitions = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ProjectionDefinitions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.ProjectionDefinitions);
    }
}
