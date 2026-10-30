// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#pragma warning disable SA1600
[DbContext(typeof(ClusterDbContext))]
[Migration("Cluster-SingleWriterLease-v19_105_0")]
public class v19_105_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var (keyType, ownerType, nameType, timeType, collation) = ActiveProvider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => ("nvarchar(64)", "nvarchar(200)", "nvarchar(150)", "datetime2(3)", "Latin1_General_100_BIN2"),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => ("character varying(64)", "character varying(200)", "character varying(150)", "timestamp(3) with time zone", "C"),
            _ => ("TEXT", "TEXT", "TEXT", "TEXT", "BINARY")
        };
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.SingleWriterLeases,
            columns: table => new
            {
                Name = table.Column<string>(type: keyType, maxLength: 64, nullable: false, collation: collation),
                Owner = table.Column<string>(type: ownerType, maxLength: 200, nullable: false, collation: collation),
                ClusterId = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                ServiceId = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                HostName = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                Version = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                AcquiredAt = table.Column<DateTime>(type: timeType, nullable: false, precision: 3),
                RenewedAt = table.Column<DateTime>(type: timeType, nullable: false, precision: 3),
                ExpiresAt = table.Column<DateTime>(type: timeType, nullable: false, precision: 3)
            },
            constraints: table => table.PrimaryKey("PK_SingleWriterLeases", row => row.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.SingleWriterLeases);
}
