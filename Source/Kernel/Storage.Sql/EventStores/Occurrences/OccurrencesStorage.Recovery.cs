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
    public async Task<bool> HasPendingForTrigger(TriggerId trigger)
    {
        await using var scope = await open();
        var hash = StorageIdentity.Hash(trigger.Value);
        return await scope.DbContext.Set<OccurrenceEntry>().AnyAsync(row => row.Trigger == hash && (row.State == OccurrenceState.Pending || row.State == OccurrenceState.Leased));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TriggerId>> GetPendingTriggers()
    {
        await using var scope = await open();

        // Trigger is a hash on SQL; read one ingress value per unfinished trigger to recover
        // its original identity, never materialize every pending occurrence and its payload.
        var data = await scope.DbContext.Set<OccurrenceEntry>().AsNoTracking()
            .Where(row => row.State == OccurrenceState.Pending || row.State == OccurrenceState.Leased)
            .GroupBy(row => row.Trigger).Select(group => group.First().Data).ToListAsync();
        return data.Select(value => JsonSerializer.Deserialize<Occurrence>(value)!.Trigger).ToArray();
    }
}
