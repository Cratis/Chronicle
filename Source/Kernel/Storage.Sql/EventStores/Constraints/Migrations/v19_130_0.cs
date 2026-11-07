// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.Migrations;

/// <summary>
/// Creates the table holding the unique-constraint encoding key.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.UniqueConstraintEncodingKey}-{nameof(v19_130_0)}")]
public class v19_130_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.UniqueConstraintEncodingKey,
            columns: table => new
            {
                Id = table.NumberColumn<int>(migrationBuilder, nullable: false),

                // Arc has no binary column helper, so the generic builder is used for the fixed-length binary columns.
                KeyId = table.Column<byte[]>(maxLength: 16, nullable: false, fixedLength: true),
                Material = table.Column<byte[]>(maxLength: 32, nullable: false, fixedLength: true)
            },
            constraints: table => table.PrimaryKey("PK_UniqueConstraintEncodingKey", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.UniqueConstraintEncodingKey);
}
