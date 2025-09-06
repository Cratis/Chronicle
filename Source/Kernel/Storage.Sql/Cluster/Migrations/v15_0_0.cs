// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ClusterDbContext))]
[Migration($"Cluster-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventStores,
            columns: table => new
            {
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventStores}", x => x.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.EventStores);
    }
}
