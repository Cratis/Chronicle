// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.WebhookDefinitions}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.WebhookDefinitions,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200),
                Owner = table.NumberColumn<int>(migrationBuilder),
                EventSequenceId = table.StringColumn(migrationBuilder),
                EventTypes = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
                Target = table.JsonColumn<WebhookTarget>(migrationBuilder),
                IsReplayable = table.BoolColumn(migrationBuilder),
                IsActive = table.BoolColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.WebhookDefinitions}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.WebhookDefinitions);
    }
}
