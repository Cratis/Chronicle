// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#pragma warning disable SA1600
[DbContext(typeof(ClusterDbContext))]
[Migration("Cluster-Membership-v19_93_0")]
public class v19_93_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var (clusterType, addressType, nameType, textType, timeType, collation) = ActiveProvider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => ("nvarchar(150)", "varchar(45)", "nvarchar(150)", "nvarchar(max)", "datetime2(3)", "Latin1_General_100_BIN2"),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => ("character varying(150)", "character varying(45)", "character varying(150)", "text", "timestamp(3) with time zone", "C"),
            _ => ("TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "BINARY")
        };
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.MembershipVersions,
            columns: table => new
            {
                ClusterId = table.Column<string>(type: clusterType, maxLength: 150, nullable: false, collation: collation),
                Version = table.Column<int>(nullable: false),
                Timestamp = table.Column<DateTime>(type: timeType, nullable: false, precision: 3)
            },
            constraints: table => table.PrimaryKey("PK_MembershipVersions", row => row.ClusterId));
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.MembershipEntries,
            columns: table => new
            {
                ClusterId = table.Column<string>(type: clusterType, maxLength: 150, nullable: false, collation: collation),
                Address = table.Column<string>(type: addressType, maxLength: 45, nullable: false),
                Port = table.Column<int>(nullable: false),
                Generation = table.Column<int>(nullable: false),
                SiloName = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                HostName = table.Column<string>(type: nameType, maxLength: 150, nullable: false),
                Status = table.Column<int>(nullable: false),
                ProxyPort = table.Column<int>(nullable: false),
                SuspectTimes = table.Column<string>(type: textType, nullable: true),
                StartTime = table.Column<DateTime>(type: timeType, nullable: false, precision: 3),
                IAmAliveTime = table.Column<DateTime>(type: timeType, nullable: false, precision: 3),
                ETag = table.Column<int>(nullable: false),
                RoleName = table.Column<string>(type: textType, nullable: true),
                UpdateZone = table.Column<int>(nullable: false),
                FaultZone = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MembershipEntries", row => new { row.ClusterId, row.Address, row.Port, row.Generation });
                table.ForeignKey("FK_MembershipEntries_Versions", row => row.ClusterId, WellKnownTableNames.MembershipVersions, "ClusterId", onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(WellKnownTableNames.MembershipEntries);
        migrationBuilder.DropTable(WellKnownTableNames.MembershipVersions);
    }
}
