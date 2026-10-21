// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Adds owned closed stream scopes while preserving legacy stream-only closures.
/// </summary>
/// <remarks>
/// The composite key includes the dimension mask to distinguish unset dimensions from empty stored values.
/// SQL Server limits clustered keys to 900 bytes; long combined dimension values can exceed this limit.
/// A hashed scope key requires a separate namespace migration backfill mechanism before replacing this key.
/// </remarks>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ClosedStreams}-{nameof(v19_38_0)}")]
public class v19_38_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddStringColumn("EventSourceId", WellKnownTableNames.ClosedStreams, maxLength: 255, nullable: false, defaultValue: string.Empty);
        migrationBuilder.AddStringColumn("EventSourceType", WellKnownTableNames.ClosedStreams, maxLength: 255, nullable: false, defaultValue: string.Empty);
        migrationBuilder.AddStringColumn("Owner", WellKnownTableNames.ClosedStreams, maxLength: 255, nullable: false, defaultValue: string.Empty);
        migrationBuilder.AddNumberColumn<int>("Dimensions", WellKnownTableNames.ClosedStreams, nullable: false, defaultValue: 12);
        var sqlite = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";

        // SQLite rounds a numeric literal outside Int64 through REAL, even in a TEXT column.
        // Its quoted text default preserves UInt64.MaxValue exactly for legacy closures.
        migrationBuilder.AddColumn<decimal>(
            name: "SequenceNumber",
            table: WellKnownTableNames.ClosedStreams,
            type: sqlite ? "TEXT" : "decimal(20,0)",
            nullable: false,
            defaultValue: sqlite ? EventSequenceNumber.Unavailable.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : (decimal)EventSequenceNumber.Unavailable.Value);
        migrationBuilder.AddDateTimeOffsetColumn("ClosedAt", WellKnownTableNames.ClosedStreams, nullable: true);
        migrationBuilder.DropPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams);
        migrationBuilder.AddPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams, ["EventSequenceId", "StreamType", "StreamId", "EventSourceId", "EventSourceType", "Owner", "Dimensions"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams);
        migrationBuilder.AddPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams, ["EventSequenceId", "StreamType", "StreamId"]);
        migrationBuilder.DropColumn("EventSourceId", WellKnownTableNames.ClosedStreams);
        migrationBuilder.DropColumn("EventSourceType", WellKnownTableNames.ClosedStreams);
        migrationBuilder.DropColumn("Owner", WellKnownTableNames.ClosedStreams);
        migrationBuilder.DropColumn("Dimensions", WellKnownTableNames.ClosedStreams);
        migrationBuilder.DropColumn("SequenceNumber", WellKnownTableNames.ClosedStreams);
        migrationBuilder.DropColumn("ClosedAt", WellKnownTableNames.ClosedStreams);
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClosedStreamEntry>(entity =>
        {
            entity.ToTable(WellKnownTableNames.ClosedStreams);
            entity.HasKey(row => new { row.EventSequenceId, row.StreamType, row.StreamId, row.EventSourceId, row.EventSourceType, row.Owner, row.Dimensions });
            entity.Property(row => row.SequenceNumber).HasPrecision(20, 0);
            entity.Property(row => row.Dimensions);
            entity.Property(row => row.ClosedAt);
        });
    }
}
