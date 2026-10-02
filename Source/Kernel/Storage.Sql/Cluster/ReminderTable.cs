// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of the reminder table for Orleans.
/// </summary>
/// <remarks>
/// Uses the migration-aware cluster scope so reminder access during re-bootstrap recreates the schema
/// after a development reset, just like the other cluster storage operations.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReminderTable(IDatabase database) : IReminderTable
{
    /// <inheritdoc/>
    /// <remarks>
    /// Reminders written by earlier versions were stored under a hash that is randomized per process, which range
    /// reads can never match. Store them under the stable hash before the reminder service starts reading ranges.
    /// </remarks>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var reminders = await dbContext.Reminders.ToListAsync(cancellationToken);
        var remindersWithStaleHash = reminders
            .Select(reminder => (Reminder: reminder, GrainHash: ReminderEntryConverters.GetGrainHash(GrainId.Parse(reminder.GrainId))))
            .Where(_ => _.Reminder.GrainHash != _.GrainHash);

        foreach (var (reminder, grainHash) in remindersWithStaleHash)
        {
            reminder.GrainHash = grainHash;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ReminderEntry?> ReadRow(GrainId grainId, string reminderName)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var reminder = await FindReminder(dbContext, grainId, reminderName);
        return reminder?.ToOrleans();
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var reminders = await dbContext.Reminders
            .Where(r => r.GrainId == grainId.ToString())
            .ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The range is exclusive of <paramref name="begin"/> and inclusive of <paramref name="end"/>. When
    /// <paramref name="begin"/> is greater than or equal to <paramref name="end"/> the range wraps around the ring,
    /// so a range where both are equal covers every reminder.
    /// </remarks>
    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var query = begin < end
            ? dbContext.Reminders.Where(r => r.GrainHash > begin && r.GrainHash <= end)
            : dbContext.Reminders.Where(r => r.GrainHash > begin || r.GrainHash <= end);
        var reminders = await query.ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var reminder = await FindReminder(dbContext, grainId, reminderName);
        if (reminder is null || reminder.ETag != eTag)
        {
            return false;
        }

        dbContext.Reminders.Remove(reminder);
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task TestOnlyClearTable()
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        dbContext.Reminders.RemoveRange(dbContext.Reminders);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<string?> UpsertRow(ReminderEntry entry)
    {
        await using var scope = await database.Cluster();
        var dbContext = scope.DbContext;
        var entity = entry.ToSql();
        var existing = await FindReminder(dbContext, entry.GrainId, entry.ReminderName);
        if (existing is not null)
        {
            // Retain readable legacy rows rather than duplicating them under the bounded key. No schema
            // migration is necessary; only new oversized composite identities use a hash.
            entity.Id = existing.Id;
        }
        await dbContext.Reminders.Upsert(entity);
        await dbContext.SaveChangesAsync();
        return entity.ETag;
    }

    static async Task<Reminder?> FindReminder(ClusterDbContext dbContext, GrainId grainId, string reminderName)
    {
        var key = ReminderEntryConverters.GetRowKey(grainId, reminderName);
        var reminder = await dbContext.Reminders.FindAsync(key);
        var legacyKey = ReminderEntryConverters.GetLegacyRowKey(grainId, reminderName);
        if (reminder is null && key != legacyKey)
        {
            reminder = await dbContext.Reminders.FindAsync(legacyKey);
            if (reminder is not null && (reminder.GrainId != grainId.ToString() || reminder.ReminderName != reminderName))
            {
                // The old delimiter-based key can belong to a different pair. Never adopt that row.
                return null;
            }
        }

        return reminder;
    }
}
