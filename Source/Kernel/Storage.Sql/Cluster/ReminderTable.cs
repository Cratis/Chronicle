// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of the reminder table for Orleans.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ReminderTable(ClusterDbContext dbContext) : IReminderTable
{
    /// <inheritdoc/>
    public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
    {
        var rowKey = ReminderEntryConverters.GetRowKey(grainId, reminderName);
        return (await dbContext.Reminders.FindAsync(rowKey))!.ToOrleans();
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        var reminders = await dbContext.Reminders
            .Where(r => r.GrainId == grainId.ToString())
            .ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        var reminders = await dbContext.Reminders
            .Where(r => r.GrainHash >= begin && r.GrainHash <= end)
            .ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
    {
        var rowKey = ReminderEntryConverters.GetRowKey(grainId, reminderName);
        var reminder = await dbContext.Reminders.FindAsync(rowKey);
        if (reminder == null || reminder.ETag != eTag)
        {
            return false;
        }

        dbContext.Reminders.Remove(reminder);
        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public Task TestOnlyClearTable()
    {
        dbContext.Reminders.RemoveRange(dbContext.Reminders);
        return dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<string> UpsertRow(ReminderEntry entry)
    {
        var entity = entry.ToSql();
        dbContext.Reminders.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity.Id;
    }
}
