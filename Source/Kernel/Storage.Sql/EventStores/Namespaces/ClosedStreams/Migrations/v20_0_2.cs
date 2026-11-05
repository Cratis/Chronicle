// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Keeps closed stream entries per exact spelling of their identifiers.
/// </summary>
/// <remarks>
/// On SQL Server the key gets a persisted hash of the stored bytes of the identifiers, so entries that differ only
/// by letter case, width or trailing spaces can coexist. Entries stored before this migration are marked as not
/// reviewed: they keep applying to every spelling until an operator reviews them. Stored identifier values are never changed.
/// </remarks>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.ClosedStreams}-{nameof(v20_0_2)}")]
public class v20_0_2 : Migration
{
    internal const string ExactKeyExpression =
        "HASHBYTES('SHA2_256', CAST(DATALENGTH([StreamType]) AS binary(2)) + CAST([StreamType] AS varbinary(510))"
        + " + CAST(DATALENGTH([StreamId]) AS binary(2)) + CAST([StreamId] AS varbinary(510))"
        + " + CAST(DATALENGTH([EventSourceId]) AS binary(2)) + CAST([EventSourceId] AS varbinary(510))"
        + " + CAST(DATALENGTH([EventSourceType]) AS binary(2)) + CAST([EventSourceType] AS varbinary(510)))";

    static readonly string[] _legacyKey = ["EventSequenceId", "StreamType", "StreamId", "EventSourceId", "EventSourceType", "Owner", "Dimensions"];
    static readonly string[] _exactKey = [.. _legacyKey, "ExactKey"];

    bool IsSqlServer => ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (IsSqlServer)
        {
            migrationBuilder.Sql(
                $"DECLARE @count bigint = (SELECT COUNT_BIG(*) FROM [{WellKnownTableNames.ClosedStreams}] "
                + "WHERE DATALENGTH([EventSequenceId]) + DATALENGTH([StreamType]) + DATALENGTH([StreamId]) "
                + "+ DATALENGTH([EventSourceId]) + DATALENGTH([EventSourceType]) + DATALENGTH([Owner]) "
                + "+ DATALENGTH([Dimensions]) + 32 > 900); "
                + "IF @count > 0 BEGIN "
                + "DECLARE @message nvarchar(2048) = CONCAT(@count, N' closed stream entries have keys too long for the new primary key. Nothing changed. Shorten or remove those entries before upgrading.'); "
                + "THROW 50000, @message, 1; END;");
        }

        // Entries written before this migration, and by nodes that do not know the column, are not reviewed on SQL Server.
        migrationBuilder.AddColumn<bool>(
            name: "ExactSpelling",
            table: WellKnownTableNames.ClosedStreams,
            nullable: false,
            defaultValue: !IsSqlServer);

        if (!IsSqlServer)
        {
            return;
        }

        migrationBuilder.Sql($"ALTER TABLE [{WellKnownTableNames.ClosedStreams}] ADD [ExactKey] AS ({ExactKeyExpression}) PERSISTED NOT NULL");
        migrationBuilder.DropPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams);
        migrationBuilder.AddPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams, _exactKey);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (IsSqlServer)
        {
            migrationBuilder.Sql(
                $"IF EXISTS (SELECT 1 FROM [{WellKnownTableNames.ClosedStreams}] GROUP BY [EventSequenceId], [StreamType], [StreamId], [EventSourceId], [EventSourceType], [Owner], [Dimensions] HAVING COUNT(*) > 1) "
                + "THROW 50000, N'Closed stream entries exist that differ only by letter case, width or trailing spaces. Remove the extra entries before downgrading.', 1");
            migrationBuilder.DropPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams);
            migrationBuilder.DropColumn("ExactKey", WellKnownTableNames.ClosedStreams);
            migrationBuilder.AddPrimaryKey($"PK_{WellKnownTableNames.ClosedStreams}", WellKnownTableNames.ClosedStreams, _legacyKey);
        }

        migrationBuilder.DropColumn("ExactSpelling", WellKnownTableNames.ClosedStreams);
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
            entity.Property(row => row.ExactSpelling);
        });
    }
}
