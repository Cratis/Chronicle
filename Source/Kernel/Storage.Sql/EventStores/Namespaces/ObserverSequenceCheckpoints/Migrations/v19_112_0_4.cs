// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints.Migrations;

/// <summary>
/// Adds dormant sequence-local observer checkpoints without backfilling existing observers.
/// </summary>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ObserverSequenceCheckpoints}-{nameof(v19_112_0_4)}")]
public class v19_112_0_4 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ObserverSequenceCheckpoints,
            columns: table => new
            {
                ObserverId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                EventSequenceId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                NextEventSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                LastHandledEventSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TailEventSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                FailedPartitionCount = table.NumberColumn<int>(migrationBuilder, nullable: false),
                IsReplaying = table.BoolColumn(migrationBuilder),
                Parked = table.DateTimeOffsetColumn(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.ObserverSequenceCheckpoints}", row => new { row.ObserverId, row.EventSequenceId }));

        AddNullableJsonColumn(migrationBuilder, "PartitionClaims");
        AddNullableJsonColumn(migrationBuilder, "ObserverWideClaim");
        AddNullableJsonColumn(migrationBuilder, "DeliveryFences");
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: WellKnownTableNames.ObserverSequenceCheckpoints);

    void AddNullableJsonColumn(MigrationBuilder migrationBuilder, string name)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // jsonb validates values itself; avoid Arc's auxiliary JSON check/index SQL.
            migrationBuilder.AddColumn<string>(name: name, table: WellKnownTableNames.ObserverSequenceCheckpoints, type: "jsonb", nullable: true);
            return;
        }

        migrationBuilder.AddJsonColumn<string>(name: name, table: WellKnownTableNames.ObserverSequenceCheckpoints);
        migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == name).IsNullable = true;
    }
}
