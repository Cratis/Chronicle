// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Creates the same infrastructure tables in each provider's application and namespace databases.
/// </summary>
internal static class OccurrenceTablesMigration
{
    /// <summary>
    /// Creates the additive tables and indexes.
    /// </summary>
    /// <param name="builder">The migration builder.</param>
    public static void Up(MigrationBuilder builder)
    {
        builder.CreateTable(
            "Occurrences",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Trigger = table.StringColumn(builder, maxLength: 64, nullable: false),
                Schedule = table.StringColumn(builder, maxLength: 64, nullable: true),
                Data = table.StringColumn(builder, nullable: false),
                State = table.NumberColumn<int>(builder, nullable: false),
                Epoch = table.NumberColumn<long>(builder, nullable: false),
                Attempts = table.NumberColumn<int>(builder, nullable: false),
                Owner = table.StringColumn(builder, nullable: true),
                Expiry = table.NumberColumn<long>(builder, nullable: true),
                NextAttempt = table.NumberColumn<long>(builder, nullable: true),
                LastError = table.StringColumn(builder, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Occurrences", row => row.Id));
        builder.CreateIndex("IX_Occurrences_Trigger_State", "Occurrences", ["Trigger", "State"]);
        builder.CreateTable(
            "OccurrenceClaimFences",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Version = table.NumberColumn<long>(builder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OccurrenceClaimFences", row => row.Id));
        builder.CreateTable(
            "ScheduleDefinitions",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Revision = table.NumberColumn<long>(builder, nullable: false),
                Data = table.StringColumn(builder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ScheduleDefinitions", row => row.Id));
        builder.CreateTable(
            "ScheduleCursors",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Token = table.StringColumn(builder, nullable: false),
                LastMaterializedDue = table.NumberColumn<long>(builder, nullable: false),
                RegisteredRevision = table.NumberColumn<long>(builder, nullable: false),
                AppliedRevision = table.NumberColumn<long>(builder, nullable: false),
                Version = table.NumberColumn<long>(builder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ScheduleCursors", row => row.Id));
        builder.CreateTable(
            "SkippedRanges",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Token = table.StringColumn(builder, maxLength: 64, nullable: false),
                Data = table.StringColumn(builder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SkippedRanges", row => row.Id));
        builder.CreateIndex("IX_SkippedRanges_Token", "SkippedRanges", "Token");
        builder.CreateTable(
            "EffectReceipts",
            table => new
            {
                Id = table.StringColumn(builder, maxLength: 64, nullable: false),
                Data = table.StringColumn(builder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EffectReceipts", row => row.Id));
    }

    /// <summary>
    /// Removes only this migration's infrastructure tables.
    /// </summary>
    /// <param name="builder">The migration builder.</param>
    public static void Down(MigrationBuilder builder)
    {
        foreach (var table in new[] { "EffectReceipts", "SkippedRanges", "ScheduleCursors", "ScheduleDefinitions", "OccurrenceClaimFences", "Occurrences" })
        {
            builder.DropTable(table);
        }
    }
}
