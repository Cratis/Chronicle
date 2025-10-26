// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Cratis.Applications.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"{WellKnownTableNames.Observers}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Observers,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                LastHandledEventSequenceNumber = table.NumberColumn<ulong>(migrationBuilder),
                RunningState = table.NumberColumn<int>(migrationBuilder),
                ReplayingPartitions = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                CatchingUpPartitions = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                FailedPartitions = table.JsonColumn<IEnumerable<FailedPartition>>(migrationBuilder),
                IsReplaying = table.BoolColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Observers}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.Observers);
    }
}
