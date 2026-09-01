// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequenceMutationHeads}-{nameof(v16_45_0)}")]
public class v16_45_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var useTextMutationIds = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequenceMutationHeads,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Coverage = table.NumberColumn<int>(migrationBuilder, nullable: false, defaultValue: 0),
                LastAssignedOrdinal = table.NumberColumn<long>(migrationBuilder, nullable: false, defaultValue: 0L),
                ActiveMutationId = useTextMutationIds
                    ? table.StringColumn(migrationBuilder, maxLength: 36, nullable: true)
                    : table.GuidColumn(migrationBuilder, nullable: true),
                ActiveOrdinal = table.NumberColumn<long>(migrationBuilder, nullable: true),
                ActiveOriginSequence = table.StringColumn(migrationBuilder, maxLength: 200, nullable: true),
                ActiveOriginSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveKind = table.NumberColumn<int>(migrationBuilder, nullable: true),
                ActiveCommandPayload = table.StringColumn(migrationBuilder, nullable: true),
                ActiveCommandHash = table.StringColumn(migrationBuilder, maxLength: 64, nullable: true),
                ActiveTargetStart = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveTargetEndExclusive = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveTargetExpectedCount = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActivePhase = table.NumberColumn<int>(migrationBuilder, nullable: true),
                ActiveBlockedFrom = table.NumberColumn<int>(migrationBuilder, nullable: true),
                ActiveRepairState = table.NumberColumn<int>(migrationBuilder, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    $"PK_{WellKnownTableNames.EventSequenceMutationHeads}",
                    x => x.EventSequenceId);
                if (useTextMutationIds)
                {
                    table.CheckConstraint(
                        $"CK_{WellKnownTableNames.EventSequenceMutationHeads}_ActiveMutationId_Text",
                        "\"ActiveMutationId\" IS NULL OR " + CanonicalSqliteMutationId("ActiveMutationId"));
                }
            });

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequenceMutationHistory,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Ordinal = table.NumberColumn<long>(migrationBuilder, nullable: false),
                MutationId = useTextMutationIds
                    ? table.StringColumn(migrationBuilder, maxLength: 36, nullable: false)
                    : table.GuidColumn(migrationBuilder, nullable: false),
                OriginSequence = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                OriginSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                Kind = table.NumberColumn<int>(migrationBuilder, nullable: false),
                CommandHash = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                TargetStart = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TargetEndExclusive = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TargetExpectedCount = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                RepairState = table.NumberColumn<int>(migrationBuilder, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    $"PK_{WellKnownTableNames.EventSequenceMutationHistory}",
                    x => new { x.EventSequenceId, x.Ordinal });
                if (useTextMutationIds)
                {
                    table.CheckConstraint(
                        $"CK_{WellKnownTableNames.EventSequenceMutationHistory}_MutationId_Text",
                        CanonicalSqliteMutationId("MutationId"));
                }
            });

        migrationBuilder.CreateIndex(
            name: $"IX_{WellKnownTableNames.EventSequenceMutationHistory}_MutationId",
            table: WellKnownTableNames.EventSequenceMutationHistory,
            column: "MutationId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequenceMutationHistory);
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequenceMutationHeads);
    }

    static string CanonicalSqliteMutationId(string column) =>
        $"typeof(\"{column}\") = 'text' AND " +
        $"length(CAST(\"{column}\" AS BLOB)) = 36 AND " +
        $"instr(\"{column}\", char(0)) = 0 AND " +
        $"length(\"{column}\") = 36 AND " +
        $"\"{column}\" = upper(\"{column}\") AND " +
        $"substr(\"{column}\", 9, 1) = '-' AND " +
        $"substr(\"{column}\", 14, 1) = '-' AND " +
        $"substr(\"{column}\", 19, 1) = '-' AND " +
        $"substr(\"{column}\", 24, 1) = '-' AND " +
        $"length(replace(\"{column}\", '-', '')) = 32 AND " +
        $"\"{column}\" NOT GLOB '*[^0-9A-F-]*'";
}
