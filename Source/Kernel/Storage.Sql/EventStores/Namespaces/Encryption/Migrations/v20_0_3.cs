// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;

/// <summary>Adds isolated authorities for external key-material stores without changing native key fences.</summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EncryptionKeys}-{nameof(v20_0_3)}")]
public class v20_0_3 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var collation = ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal) ? EncryptionKeyIdentifierColumn.Collation : null;
        migrationBuilder.CreateTable(
            "EncryptionKeyExternalFences",
            columns: table => new
            {
                Identifier = table.Column<string>(maxLength: 256, nullable: false, collation: collation),
                Version = table.Column<decimal>(nullable: false, precision: 20, scale: 0),
                State = table.Column<string>(nullable: false),
                Seal = table.Column<string>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EncryptionKeyExternalFences", _ => _.Identifier));
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("EncryptionKeyExternalFences");
}
