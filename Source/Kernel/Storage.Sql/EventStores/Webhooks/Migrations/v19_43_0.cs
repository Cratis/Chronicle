// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.WebhookDefinitions}-{nameof(v19_43_0)}")]
public class v19_43_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(
        name: nameof(WebhookDefinition.GenerationDelivery), table: WellKnownTableNames.WebhookDefinitions, nullable: false, defaultValue: 0);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: nameof(WebhookDefinition.GenerationDelivery), table: WellKnownTableNames.WebhookDefinitions);
}
