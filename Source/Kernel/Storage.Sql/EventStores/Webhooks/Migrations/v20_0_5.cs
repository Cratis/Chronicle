// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations;

/// <summary>
/// Persists the opt-in signing policy without enabling signing for existing webhooks.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.WebhookDefinitions}-{nameof(v20_0_5)}")]
public class v20_0_5 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<int>(
            name: nameof(WebhookDefinition.Signing),
            table: WellKnownTableNames.WebhookDefinitions,
            nullable: false,
            defaultValue: 0);

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: nameof(WebhookDefinition.Signing), table: WellKnownTableNames.WebhookDefinitions);
}
