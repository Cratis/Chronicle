// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReadModelDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReadModelDefinitions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Name = table.StringColumn(migrationBuilder),
                Owner = table.NumberColumn<int>(migrationBuilder),
                Source = table.NumberColumn<int>(migrationBuilder, nullable: false, defaultValue: 0),
                ObserverType = table.NumberColumn<int>(migrationBuilder, nullable: false, defaultValue: 0),
                ObserverIdentifier = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false, defaultValue: string.Empty),
                DisplayName = table.StringColumn(migrationBuilder, nullable: false, defaultValue: string.Empty),
                SinkType = table.StringColumn(migrationBuilder),
                SinkConfigurationId = table.GuidColumn(migrationBuilder),
                Schemas = table.JsonColumn<IDictionary<string, string>>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReadModelDefinitions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.ReadModelDefinitions);
    }
}
