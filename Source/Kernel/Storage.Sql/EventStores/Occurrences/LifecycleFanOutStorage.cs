// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Stores lifecycle snapshots in the event-store database with insert-if-absent semantics.
/// </summary>
/// <param name="open">Opens an event-store context for each operation.</param>
public class LifecycleFanOutStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open) : ILifecycleFanOutStorage
{
    /// <inheritdoc/>
    public async Task<LifecycleFanOutIntent?> Get(OccurrenceId id)
    {
        await using var scope = await open();
        var row = await scope.DbContext.Set<LifecycleFanOutEntry>().AsNoTracking().SingleOrDefaultAsync(_ => _.Id == id.Value);
        return row is null ? null : FromEntry(row);
    }

    /// <inheritdoc/>
    public async Task<bool> TryInsert(LifecycleFanOutIntent intent)
    {
        await using var scope = await open();
        scope.DbContext.Set<LifecycleFanOutEntry>().Add(new() { Id = intent.Occurrence.Id.Value, Data = JsonSerializer.Serialize(intent), Count = intent.Namespaces.Count, Progress = intent.Progress });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LifecycleFanOutIntent>> GetPending(int max)
    {
        await using var scope = await open();
        var rows = await scope.DbContext.Set<LifecycleFanOutEntry>().AsNoTracking().Where(_ => _.Progress < _.Count).OrderBy(_ => _.Id).Take(Math.Clamp(max, 0, 100)).ToListAsync();
        return rows.Select(FromEntry).ToArray();
    }

    /// <inheritdoc/>
    public async Task Advance(OccurrenceId id, int progress)
    {
        await using var scope = await open();
        await scope.DbContext.Set<LifecycleFanOutEntry>().Where(_ => _.Id == id.Value && _.Progress < progress && _.Count >= progress).ExecuteUpdateAsync(set => set.SetProperty(_ => _.Progress, progress));
    }

    static LifecycleFanOutIntent FromEntry(LifecycleFanOutEntry row) => JsonSerializer.Deserialize<LifecycleFanOutIntent>(row.Data)! with { Progress = row.Progress };
}
