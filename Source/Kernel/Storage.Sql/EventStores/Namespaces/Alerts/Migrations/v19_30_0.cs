// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.AlertIncidents}-{nameof(v19_30_0)}")]
public class v19_30_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var sqlServer = ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal);
        var postgres = ActiveProvider.Contains("Npgsql", StringComparison.Ordinal);
        string collation;
        if (sqlServer)
        {
            collation = "Latin1_General_100_BIN2";
        }
        else if (postgres)
        {
            collation = "C";
        }
        else
        {
            collation = "BINARY";
        }

        var idType = sqlServer ? "nvarchar(32)" : "TEXT";
        var keyType = sqlServer ? "nvarchar(200)" : "TEXT";
        var textType = sqlServer ? "nvarchar(max)" : "TEXT";
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.AlertIncidents,
            columns: table => new
            {
                Id = table.Column<string>(type: idType, nullable: false, collation: collation),
                EventStore = table.Column<string>(type: keyType, nullable: false, collation: collation),
                Namespace = table.Column<string>(type: keyType, nullable: false, collation: collation),
                ObserverId = table.Column<string>(type: textType, nullable: false, collation: collation),
                EventSequenceId = table.Column<string>(type: textType, nullable: false, collation: collation),
                Partition = table.Column<string>(type: textType, nullable: false, collation: collation),
                Condition = table.Column<string>(type: textType, nullable: false, collation: collation),
                Severity = table.Column<int>(nullable: true),
                AttemptCount = table.Column<int>(nullable: true),
                FirstFailure = table.Column<string>(type: textType, nullable: true, collation: collation),
                LastFailure = table.Column<string>(type: textType, nullable: true, collation: collation),
                FailureKind = table.Column<int>(nullable: true),
                Message = table.Column<string>(type: textType, nullable: true, collation: collation),
                RaisedAt = table.Column<string>(type: textType, nullable: true, collation: collation),
                RaisedSequenceNumber = table.Column<long>(nullable: true),
                LastChangedAt = table.Column<string>(type: textType, nullable: false, collation: collation),
                LastTransitionSequenceNumber = table.Column<long>(nullable: false),
                IsOpen = table.Column<bool>(nullable: false),
                ClearedReason = table.Column<int>(nullable: true)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.AlertIncidents}", row => row.Id));
        migrationBuilder.CreateIndex(name: "IX_AlertIncidents_Store_Open_Raised_Id", table: WellKnownTableNames.AlertIncidents, columns: ["EventStore", "IsOpen", "RaisedSequenceNumber", "Id"]);
        migrationBuilder.CreateIndex(name: "IX_AlertIncidents_Store_Namespace_Open_Raised_Id", table: WellKnownTableNames.AlertIncidents, columns: ["EventStore", "Namespace", "IsOpen", "RaisedSequenceNumber", "Id"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.AlertIncidents);
}
