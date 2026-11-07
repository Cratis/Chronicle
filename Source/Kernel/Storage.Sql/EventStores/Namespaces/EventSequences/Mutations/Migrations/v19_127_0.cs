// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequenceMutationHeads}-{nameof(v19_127_0)}")]
public class v19_127_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var useTextMutationIds = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
        var useBinarySequenceKeys = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequenceMutationHeads,
            columns: table => new
            {
                EventSequenceId = useBinarySequenceKeys
                    ? table.Column<byte[]>(type: "varbinary(600)", maxLength: 600, nullable: false)
                    : table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Coverage = table.NumberColumn<int>(migrationBuilder, nullable: false, defaultValue: 0),
                LastAssignedOrdinal = table.NumberColumn<long>(migrationBuilder, nullable: false, defaultValue: 0L),
                ActiveMutationId = useTextMutationIds
                    ? table.StringColumn(migrationBuilder, maxLength: 36, nullable: true)
                    : table.GuidColumn(migrationBuilder, nullable: true),
                ActiveOrdinal = table.NumberColumn<long>(migrationBuilder, nullable: true),
                ActiveStateVersion = table.NumberColumn<long>(migrationBuilder, nullable: true),
                ActiveOriginSequence = table.StringColumn(migrationBuilder, maxLength: 200, nullable: true),
                ActiveOriginSequenceNumber = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveKind = table.NumberColumn<int>(migrationBuilder, nullable: true),
                ActiveCommandPayload = table.StringColumn(migrationBuilder, nullable: true),
                ActiveCommandHash = table.StringColumn(migrationBuilder, maxLength: 64, nullable: true),
                ActiveTargetStart = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveTargetEndExclusive = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveTargetExpectedCount = table.NumberColumn<ulong>(migrationBuilder, nullable: true),
                ActiveDefinitionDigestV1 = table.StringColumn(migrationBuilder, maxLength: 64, nullable: true),
                ActiveDefinitionDigestVersion = table.NumberColumn<int>(migrationBuilder, nullable: true),
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
                EventSequenceId = useBinarySequenceKeys
                    ? table.Column<byte[]>(type: "varbinary(600)", maxLength: 600, nullable: false)
                    : table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Ordinal = table.NumberColumn<long>(migrationBuilder, nullable: false),
                MutationId = useTextMutationIds
                    ? table.StringColumn(migrationBuilder, maxLength: 36, nullable: false)
                    : table.GuidColumn(migrationBuilder, nullable: false),
                Kind = table.NumberColumn<int>(migrationBuilder, nullable: false),
                TargetStart = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TargetEndExclusive = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TargetExpectedCount = table.NumberColumn<ulong>(migrationBuilder, nullable: false),
                TerminalPhase = table.NumberColumn<int>(migrationBuilder, nullable: false),
                RepairState = table.NumberColumn<int>(migrationBuilder, nullable: false),
                FinalStateVersion = table.NumberColumn<long>(migrationBuilder, nullable: false),
                DefinitionDigestV1 = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                ReceiptDigestV1 = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                DigestVersion = table.NumberColumn<int>(migrationBuilder, nullable: false),
                ArchivedAt = table.NumberColumn<long>(migrationBuilder, nullable: false)
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

        CreateInternalSecrets(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.InternalSecrets);
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequenceMutationHistory);
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequenceMutationHeads);
    }

    /// <summary>
    /// Creates the table for kernel-internal secrets, such as the one mutation receipt digests are keyed from. It is
    /// kept apart from subject encryption keys.
    /// </summary>
    /// <param name="migrationBuilder">The <see cref="MigrationBuilder"/>.</param>
    static void CreateInternalSecrets(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.InternalSecrets,
            columns: table => new
            {
                Purpose = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
                PublicKey = table.Column<byte[]>(nullable: false),
                PrivateKey = table.Column<byte[]>(nullable: false)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.InternalSecrets}", x => x.Purpose));

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
