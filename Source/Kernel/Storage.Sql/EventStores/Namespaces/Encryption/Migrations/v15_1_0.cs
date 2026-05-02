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
[Migration($"NS-{WellKnownTableNames.EncryptionKeys}-{nameof(v15_1_0)}")]
public class v15_1_0 : Migration
{
    const string TempTableName = "EncryptionKeysNew";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: TempTableName,
            columns: table => new
            {
                Identifier = table.StringColumn(migrationBuilder),
                Revision = table.NumberColumn<uint>(migrationBuilder),
                PublicKey = table.Column<byte[]>(nullable: false),
                PrivateKey = table.Column<byte[]>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{TempTableName}", x => new { x.Identifier, x.Revision }));

        migrationBuilder.Sql(
            $"INSERT INTO {TempTableName} (Identifier, Revision, PublicKey, PrivateKey) " +
            $"SELECT Identifier, 1, PublicKey, PrivateKey FROM {WellKnownTableNames.EncryptionKeys}");

        migrationBuilder.DropTable(name: WellKnownTableNames.EncryptionKeys);

        migrationBuilder.RenameTable(name: TempTableName, newName: WellKnownTableNames.EncryptionKeys);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        const string TempDownTable = "EncryptionKeysOld";

        migrationBuilder.CreateTable(
            name: TempDownTable,
            columns: table => new
            {
                Identifier = table.StringColumn(migrationBuilder),
                PublicKey = table.Column<byte[]>(nullable: false),
                PrivateKey = table.Column<byte[]>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{TempDownTable}", x => x.Identifier));

        migrationBuilder.Sql(
            $"INSERT INTO {TempDownTable} (Identifier, PublicKey, PrivateKey) " +
            $"SELECT Identifier, PublicKey, PrivateKey FROM {WellKnownTableNames.EncryptionKeys} " +
            $"WHERE Revision = (SELECT MAX(Revision) FROM {WellKnownTableNames.EncryptionKeys} e2 WHERE e2.Identifier = {WellKnownTableNames.EncryptionKeys}.Identifier)");

        migrationBuilder.DropTable(name: WellKnownTableNames.EncryptionKeys);

        migrationBuilder.RenameTable(name: TempDownTable, newName: WellKnownTableNames.EncryptionKeys);
    }
}
