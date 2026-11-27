// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Migrations;

/// <summary>Creates the namespace-local conflict marker without admitting a lifetime.</summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{NamespaceMarkerRow.TableName}-{nameof(v20_0_7)}")]
public class v20_0_7 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: NamespaceMarkerRow.TableName,
            columns: table => new
            {
                Id = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Incarnation = table.Column<Guid>(nullable: false),
                IsOpen = table.BoolColumn(migrationBuilder),
                Touch = table.NumberColumn<long>(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_NamespaceMarker", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(NamespaceMarkerRow.TableName);
}
