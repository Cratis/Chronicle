// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
    public static string GetRowKey(GrainId grainId, string reminderName)
    {
        var legacyKey = GetLegacyRowKey(grainId, reminderName);
        if (legacyKey.Length <= 200) return legacyKey;

        // A serialized pair avoids delimiter ambiguity. The hash bounds the complete key, including long grain IDs.
        var identity = JsonSerializer.Serialize(new[] { grainId.ToString(), reminderName });
        return $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))}";
    }

    /// <summary>
    /// Gets the composite key used by earlier versions, including oversized keys accepted by SQLite.
    /// </summary>
    /// <param name="grainId">The grain identifier.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <returns>The legacy row key.</returns>
    internal static string GetLegacyRowKey(GrainId grainId, string reminderName) => $"{grainId}-{reminderName}";

    /// <summary>
    /// Gets the hash a reminder is stored under, which is what the reminder service reads ranges of reminders by.
    /// </summary>
    /// <param name="grainId">The grain identifier.</param>
    /// <returns>The grain hash.</returns>
    /// <remarks>
    /// This has to be <see cref="GrainId.GetUniformHashCode"/>: it is stable across processes and is the hash the
    /// reminder service divides its ring by. <see cref="GrainId.GetHashCode"/> is randomized per process.
    /// </remarks>
    public static uint GetGrainHash(GrainId grainId) => grainId.GetUniformHashCode();

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
    /// Orleans hands the table a fresh entry with a null ETag on first insert and expects the
    /// table to generate one. Generate a new ETag when the entry has none.
    /// </summary>
    /// <param name="entry">The Orleans reminder entry.</param>
    /// <returns>The SQL reminder entity.</returns>
    public static Reminder ToSql(this ReminderEntry entry) =>
        new()
        {
            Id = GetRowKey(entry.GrainId, entry.ReminderName),
            GrainId = entry.GrainId.ToString(),
            GrainHash = GetGrainHash(entry.GrainId),
            ReminderName = entry.ReminderName,
            ETag = string.IsNullOrEmpty(entry.ETag) ? Guid.NewGuid().ToString("N") : entry.ETag,
            StartAt = entry.StartAt.ToBinary(),
            Period = (long)entry.Period.TotalMilliseconds
        };
}
