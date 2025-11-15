// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Cratis.Applications.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequences}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create EventSequences table for storing event sequence state
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequences,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder),
                SequenceNumber = table.Column<decimal>(nullable: false),
                TailSequenceNumberPerEventType = table.JsonColumn<IDictionary<string, object>>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventSequences}", x => x.EventSequenceId));

        // Create EventSequenceEvents table for storing individual events
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequenceEvents,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder),
                SequenceNumber = table.Column<decimal>(nullable: false),
                CorrelationId = table.StringColumn(migrationBuilder),
                Causation = table.StringColumn(migrationBuilder),
                CausedBy = table.StringColumn(migrationBuilder),
                EventTypeId = table.StringColumn(migrationBuilder),
                EventTypeGeneration = table.Column<decimal>(nullable: false),
                Occurred = table.Column<DateTimeOffset>(nullable: false),
                ValidFrom = table.Column<DateTimeOffset>(nullable: false),
                ValidTo = table.Column<DateTimeOffset>(nullable: true),
                EventSourceType = table.StringColumn(migrationBuilder),
                EventSourceId = table.StringColumn(migrationBuilder),
                EventStreamType = table.StringColumn(migrationBuilder),
                EventStreamId = table.StringColumn(migrationBuilder),
                Content = table.StringColumn(migrationBuilder),
                Compensations = table.JsonColumn<IDictionary<string, string>>(migrationBuilder)
            },
            constraints: table =>
            {
                table.PrimaryKey($"PK_{WellKnownTableNames.EventSequenceEvents}", x => new { x.EventSequenceId, x.SequenceNumber });
                table.ForeignKey(
                    name: $"FK_{WellKnownTableNames.EventSequenceEvents}_{WellKnownTableNames.EventSequences}_EventSequenceId",
                    column: x => x.EventSequenceId,
                    principalTable: WellKnownTableNames.EventSequences,
                    principalColumn: "EventSequenceId",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for common query patterns
        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceEvents}_EventSequenceId_SequenceNumber",
            table: WellKnownTableNames.EventSequenceEvents,
            columns: ["EventSequenceId", "SequenceNumber"]);

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceEvents}_EventSourceId",
            table: WellKnownTableNames.EventSequenceEvents,
            column: "EventSourceId");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceEvents}_Type",
            table: WellKnownTableNames.EventSequenceEvents,
            column: "Type");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceEvents}_Occurred",
            table: WellKnownTableNames.EventSequenceEvents,
            column: "Occurred");

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceEvents}_EventStreamType_EventStreamId",
            table: WellKnownTableNames.EventSequenceEvents,
            columns: ["EventStreamType", "EventStreamId"]);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequenceEvents);
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequences);
    }
}
