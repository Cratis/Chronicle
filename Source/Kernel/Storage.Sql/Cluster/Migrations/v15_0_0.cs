// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
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
                Name = table.StringColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventStores}", x => x.Name));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Reminders,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                GrainId = table.StringColumn(migrationBuilder),
                GrainHash = table.NumberColumn<uint>(migrationBuilder),
                ReminderName = table.StringColumn(migrationBuilder),
                Period = table.NumberColumn<long>(migrationBuilder),
                StartAt = table.NumberColumn<long>(migrationBuilder),
                ETag = table.StringColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Reminders}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.EventStores);

        migrationBuilder.DropTable(
            name: WellKnownTableNames.Reminders);
    }
}
