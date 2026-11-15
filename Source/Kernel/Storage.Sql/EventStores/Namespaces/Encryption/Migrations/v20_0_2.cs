// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;

/// <summary>
/// Adds nullable fence and publication metadata without changing legacy key identities or material.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EncryptionKeys}-{nameof(v20_0_2)}")]
public class v20_0_2 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>("Version", "EncryptionKeyErasures", nullable: true, precision: 20, scale: 0);
        migrationBuilder.AddColumn<long>("Lifecycle", "EncryptionKeyErasures", nullable: true);
        migrationBuilder.AddColumn<long>("ErasedLifecycle", "EncryptionKeyErasures", nullable: true);
        migrationBuilder.AddColumn<string>("StateSeal", "EncryptionKeyErasures", nullable: true);
        migrationBuilder.AddColumn<string>("Requests", "EncryptionKeyErasures", nullable: true);
        migrationBuilder.AddColumn<long>("StagedLifecycle", "EncryptionKeys", nullable: true);
        migrationBuilder.AddColumn<string>("WriteNonce", "EncryptionKeys", nullable: true);
        migrationBuilder.AddColumn<bool>("Published", "EncryptionKeys", nullable: true);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("Version", "EncryptionKeyErasures");
        migrationBuilder.DropColumn("Lifecycle", "EncryptionKeyErasures");
        migrationBuilder.DropColumn("ErasedLifecycle", "EncryptionKeyErasures");
        migrationBuilder.DropColumn("StateSeal", "EncryptionKeyErasures");
        migrationBuilder.DropColumn("Requests", "EncryptionKeyErasures");
        migrationBuilder.DropColumn("StagedLifecycle", "EncryptionKeys");
        migrationBuilder.DropColumn("WriteNonce", "EncryptionKeys");
        migrationBuilder.DropColumn("Published", "EncryptionKeys");
    }
}
