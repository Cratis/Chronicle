// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents an implementation of the reminder table for Orleans.
/// </summary>
/// <param name="dbContextFactory">The <see cref="IDbContextFactory{TContext}"/> for creating <see cref="ClusterDbContext"/> instances.</param>
public class ReminderTable(IDbContextFactory<ClusterDbContext> dbContextFactory) : IReminderTable
{
    /// <inheritdoc/>
    public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var rowKey = ReminderEntryConverters.GetRowKey(grainId, reminderName);
        return (await dbContext.Reminders.FindAsync(rowKey))!.ToOrleans();
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var reminders = await dbContext.Reminders
            .Where(r => r.GrainId == grainId.ToString())
            .ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var reminders = await dbContext.Reminders
            .Where(r => r.GrainHash >= begin && r.GrainHash <= end)
            .ToListAsync();

        return new ReminderTableData(reminders.Select(r => r.ToOrleans()));
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
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
    public async Task TestOnlyClearTable()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Reminders.RemoveRange(dbContext.Reminders);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<string> UpsertRow(ReminderEntry entry)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = entry.ToSql();
        await dbContext.Reminders.Upsert(entity);
        await dbContext.SaveChangesAsync();
        return entity.Id;
    }
}
