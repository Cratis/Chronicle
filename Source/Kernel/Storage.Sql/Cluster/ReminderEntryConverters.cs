// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Converts between Orleans and SQL representations of reminder entries.
/// </summary>
public static class ReminderEntryConverters
{
    /// <summary>
    /// Gets the row key for a reminder entry.
    /// </summary>
    /// <param name="grainId">The grain identifier.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <returns>The row key.</returns>
    public static string GetRowKey(GrainId grainId, string reminderName) => $"{grainId}-{reminderName}";

    /// <summary>
    /// Converts a SQL reminder entity to an Orleans reminder entry.
    /// </summary>
    /// <param name="entity">The SQL reminder entity.</param>
    /// <returns>The Orleans reminder entry.</returns>
    public static ReminderEntry ToOrleans(this Reminder entity) =>
        new()
        {
            GrainId = GrainId.Parse(entity.GrainId),
            ReminderName = entity.ReminderName,
            ETag = entity.ETag,
            StartAt = DateTime.FromBinary(entity.StartAt),
            Period = TimeSpan.FromMilliseconds(entity.Period)
        };

    /// <summary>
    /// Converts an Orleans reminder entry to a SQL reminder entity.
    /// </summary>
    /// <param name="entry">The Orleans reminder entry.</param>
    /// <returns>The SQL reminder entity.</returns>
    public static Reminder ToSql(this ReminderEntry entry) =>
        new()
        {
            Id = GetRowKey(entry.GrainId, entry.ReminderName),
            GrainId = entry.GrainId.ToString(),
            GrainHash = (uint)entry.GrainId.GetHashCode(),
            ReminderName = entry.ReminderName,
            ETag = entry.ETag,
            StartAt = entry.StartAt.ToBinary(),
            Period = entry.Period.Milliseconds
        };
}
