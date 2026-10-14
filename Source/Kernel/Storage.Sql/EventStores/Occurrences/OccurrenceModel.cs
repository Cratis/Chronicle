// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Maps additive infrastructure tables in both application and namespace scopes.
/// </summary>
public static class OccurrenceModel
{
    /// <summary>
    /// Maps the occurrence tables on a context.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void Configure(ModelBuilder builder)
    {
        Configure<OccurrenceEntry>(builder, "Occurrences");
        Configure<OccurrenceClaimFence>(builder, "OccurrenceClaimFences");
        Configure<ScheduleDefinitionEntry>(builder, "ScheduleDefinitions");
        Configure<ScheduleCursorEntry>(builder, "ScheduleCursors");
        Configure<SkippedRangeEntry>(builder, "SkippedRanges");
        ConfigureReceipts(builder);
        builder.Entity<OccurrenceEntry>().Property(row => row.Trigger).HasMaxLength(64);
        builder.Entity<OccurrenceEntry>().Property(row => row.Schedule).HasMaxLength(64);
        builder.Entity<OccurrenceEntry>().HasIndex(row => new { row.Trigger, row.State });
        builder.Entity<SkippedRangeEntry>().Property(row => row.Token).HasMaxLength(64);
        builder.Entity<SkippedRangeEntry>().HasIndex(row => row.Token);
    }

    /// <summary>
    /// Maps receipts alongside an event sequence's events.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ConfigureReceipts(ModelBuilder builder) => Configure<EffectReceiptEntry>(builder, "EffectReceipts");

    static void Configure<T>(ModelBuilder builder, string table)
        where T : class =>
        builder.Entity<T>(entity =>
        {
            entity.ToTable(table);
            entity.HasKey("Id");
            entity.Property<string>("Id").HasMaxLength(64).ValueGeneratedNever();
        });
}
