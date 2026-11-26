// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.EventStoreSubscriptions}-{nameof(v19_160_0)}")]
public class v19_160_0 : Migration
{
    /// <summary>
    /// The column is no longer mapped: the source namespace restriction it held was removed. It stays in the table, nullable
    /// and unread, so releases that still map it keep working during a rolling upgrade, and this migration stays what it was.
    /// </summary>
    const string SourceNamespacesColumn = "SourceNamespaces";

    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: SourceNamespacesColumn, table: WellKnownTableNames.EventStoreSubscriptions, nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: SourceNamespacesColumn, table: WellKnownTableNames.EventStoreSubscriptions);
}
