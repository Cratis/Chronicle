// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations;

/// <summary>
/// Creates the table holding webhook signing keys.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.WebhookSigningKeys}-{nameof(v19_126_0)}")]
public class v19_126_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.WebhookSigningKeys,
            columns: table => new
            {
                KeyId = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                Algorithm = table.NumberColumn<int>(migrationBuilder),
                Status = table.NumberColumn<int>(migrationBuilder),
                PublicKey = table.StringColumn(migrationBuilder, nullable: false),
                EncryptedSecret = table.StringColumn(migrationBuilder, nullable: false),
                Webhook = table.StringColumn(migrationBuilder, nullable: true),
                CreatedAt = table.DateTimeOffsetColumn(migrationBuilder),
                OverlapEndsAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                RetiredAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_WebhookSigningKeys", row => row.KeyId));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.WebhookSigningKeys);
}
