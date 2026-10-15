// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

public partial class OccurrencesStorage<TContext>
{
    /// <inheritdoc/>
    public async Task<bool> RetryQuarantined(OccurrenceId id, string principal, DateTimeOffset now)
    {
        var occurrence = await Get(id);
        if (occurrence is null || occurrence.State != OccurrenceState.Quarantined)
        {
            return false;
        }

        var data = JsonSerializer.Serialize(occurrence with { RetriedBy = principal, RetriedAt = now });
        var hash = StorageIdentity.Hash(id.Value);
        await using var scope = await open();
        return await scope.DbContext.Set<OccurrenceEntry>().Where(row => row.Id == hash && row.State == OccurrenceState.Quarantined && row.Epoch == occurrence.Epoch)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.State, OccurrenceState.Pending).SetProperty(row => row.Attempts, 0).SetProperty(row => row.NextAttempt, (long?)null).SetProperty(row => row.Data, data)) == 1;
    }

    /// <inheritdoc/>
    public async Task ExpireLeases(string worker, DateTimeOffset now)
    {
        await using var scope = await open();
        var ticks = now.UtcTicks;
        await scope.DbContext.Set<OccurrenceEntry>().Where(row => row.State == OccurrenceState.Leased && row.Owner == worker && row.Expiry > ticks)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Expiry, ticks));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TriggerId>> GetPendingTriggers()
    {
        await using var scope = await open();
        var rows = await scope.DbContext.Set<OccurrenceEntry>().AsNoTracking().Where(row => row.State == OccurrenceState.Pending || row.State == OccurrenceState.Leased).ToListAsync();
        return rows.Select(row => row.ToOccurrence().Trigger).Distinct().ToArray();
    }
}
