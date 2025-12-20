// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"{WellKnownTableNames.ObserverDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ObserverDefinitions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                Owner = table.NumberColumn<int>(migrationBuilder),
                EventSequenceId = table.StringColumn(migrationBuilder),
                EventTypes = table.JsonColumn<IEnumerable<EventTypeWithKeyExpression>>(migrationBuilder),
                Type = table.NumberColumn<int>(migrationBuilder),
                IsReplayable = table.BoolColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ObserverDefinitions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.ObserverDefinitions);
    }
}
