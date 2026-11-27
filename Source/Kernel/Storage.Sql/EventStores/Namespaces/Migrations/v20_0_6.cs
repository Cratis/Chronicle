// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Migrations;

/// <summary>
/// Creates durable lifecycle tombstones outside the removable namespace listing.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{NamespaceLifecycleRow.TableName}-{nameof(v20_0_6)}")]
public class v20_0_6 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: NamespaceLifecycleRow.TableName,
            columns: table => new
            {
                NameKey = table.Column<byte[]>(maxLength: 800, nullable: false),
                Revision = table.NumberColumn<long>(migrationBuilder, nullable: false),
                Payload = table.StringColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_NamespaceLifecycles", row => row.NameKey));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(NamespaceLifecycleRow.TableName);
}
