// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds an index leading with the open state, so open incident queries do not scan retained tombstones.
/// EventStore and Namespace are bounded (nvarchar(200)) on SQL Server, so they are indexable.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.AlertIncidents}-{nameof(v19_33_0)}")]
public class v19_33_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(name: "IX_AlertIncidents_Open_Store_Namespace", table: WellKnownTableNames.AlertIncidents, columns: ["IsOpen", "EventStore", "Namespace"]);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(name: "IX_AlertIncidents_Open_Store_Namespace", table: WellKnownTableNames.AlertIncidents);
}
