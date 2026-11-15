// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;

/// <summary>Adds isolated erasure request archives and silo protocol records.</summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EncryptionKeys}-{nameof(v20_0_4)}")]
public class v20_0_4 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        "EncryptionKeyFenceRecords",
        columns: table => new
        {
            Id = table.Column<string>(maxLength: 256, nullable: false),
            Value = table.Column<string>(nullable: false),
            Seal = table.Column<string>(nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_EncryptionKeyFenceRecords", _ => _.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("EncryptionKeyFenceRecords");
}
