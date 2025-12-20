// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Reducers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReducerDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ReducerDefinitions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                EventSequenceId = table.StringColumn(migrationBuilder),
                EventTypes = table.JsonColumn<IEnumerable<EventTypeWithKeyExpression>>(migrationBuilder),
                ReadModel = table.StringColumn(migrationBuilder),
                SinkType = table.GuidColumn(migrationBuilder),
                SinkConfigurationId = table.GuidColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ReducerDefinitions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.ReducerDefinitions);
    }
}
