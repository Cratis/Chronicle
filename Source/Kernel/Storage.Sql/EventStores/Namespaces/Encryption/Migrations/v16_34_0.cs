// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EncryptionKeyErasures}-{nameof(v16_34_0)}")]
public class v16_34_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EncryptionKeyErasures,
            columns: table => new
            {
                Identifier = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                ErasedThrough = table.NumberColumn<uint>(migrationBuilder, nullable: false),
                ErasedKeyFingerprints = table.StringColumn(migrationBuilder),
                NewKeyAllowed = table.Column<bool>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EncryptionKeyErasures}", x => x.Identifier));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.EncryptionKeyErasures);
    }
}
