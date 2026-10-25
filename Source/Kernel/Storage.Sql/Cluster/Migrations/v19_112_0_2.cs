// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#pragma warning disable SA1600
[DbContext(typeof(ClusterDbContext))]
[Migration("Cluster-AdministratorPrivileges-v19_112_0_2")]
public class v19_112_0_2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Administrators,
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Reason = table.Column<int>(nullable: false),
                Evidence = table.Column<string>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Administrators", row => row.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.Administrators);
}
