// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#pragma warning disable SA1600
[DbContext(typeof(ClusterDbContext))]
[Migration("Cluster-DatabaseNames-v19_51_0")]
public class v19_51_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var sqlServer = ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
        var postgres = ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL";
        var (keyType, nameType, collation) = ActiveProvider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => ("nvarchar(200)", "nvarchar(128)", "Latin1_General_100_BIN2"),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => ("character varying(200)", "character varying(128)", "C"),
            _ => ("TEXT", "TEXT", "BINARY")
        };
        migrationBuilder.CreateTable(
            name: "DatabaseNames",
            columns: table => new
            {
                Kind = table.Column<int>(nullable: false),
                EventStore = table.Column<string>(type: keyType, nullable: false, collation: collation),
                Namespace = table.Column<string>(type: keyType, nullable: false, collation: collation),
                PhysicalName = table.Column<string>(type: nameType, nullable: false, collation: postgres ? "C" : null)
            },
            constraints: table => table.PrimaryKey("PK_DatabaseNames", row => new { row.Kind, row.EventStore, row.Namespace }));

        if (sqlServer)
        {
            // Database-name equality is the SERVER collation, not necessarily the cluster database collation.
            migrationBuilder.Sql("DECLARE @sql nvarchar(max) = N'ALTER TABLE [DatabaseNames] ALTER COLUMN [PhysicalName] nvarchar(128) COLLATE ' + CONVERT(nvarchar(128), SERVERPROPERTY('Collation')) + N' NOT NULL'; EXEC sp_executesql @sql;");
        }

        migrationBuilder.CreateIndex(name: "IX_DatabaseNames_PhysicalName", table: "DatabaseNames", column: "PhysicalName", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("DatabaseNames");
}
