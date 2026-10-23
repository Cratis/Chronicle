// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Stores infrastructure occurrences directly in one application or namespace database.
/// </summary>
/// <param name="open">Opens a fresh context for each operation.</param>
/// <typeparam name="TContext">The owning database context.</typeparam>
public partial class OccurrencesStorage<TContext>(Func<Task<DbContextScope<TContext>>> open) : IOccurrencesStorage, IScheduleStorage
    where TContext : DbContext
{
    /// <inheritdoc/>
    public async Task<OccurrenceRecordResult> Record(Occurrence occurrence)
    {
        try
        {
            await using var scope = await open();
            scope.DbContext.Set<OccurrenceEntry>().Add(OccurrenceEntry.From(occurrence));
            await scope.DbContext.SaveChangesAsync();
            return OccurrenceRecordResult.Recorded;
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            var existing = await Get(occurrence.Id);
            if (existing is null)
            {
                throw;
            }

            if (existing.Fingerprint == occurrence.Fingerprint)
            {
                SqlStorageTelemetry.RecoveredCollision("occurrence");
                return OccurrenceRecordResult.AlreadyRecorded;
            }

            return OccurrenceRecordResult.OccurrenceIdConflict;
        }
    }

    /// <inheritdoc/>
    public async Task<Occurrence?> Get(OccurrenceId id)
    {
        await using var scope = await open();
        var hash = StorageIdentity.Hash(id.Value);
        var row = await scope.DbContext.Set<OccurrenceEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == hash);
        return row?.ToOccurrence();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Occurrence>> GetForTrigger(TriggerId trigger)
    {
        await using var scope = await open();
        var hash = StorageIdentity.Hash(trigger.Value);
        var rows = await scope.DbContext.Set<OccurrenceEntry>().AsNoTracking().Where(row => row.Trigger == hash).ToListAsync();
        return rows.Select(row => row.ToOccurrence()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Occurrence>> Claim(TriggerId trigger, string worker, DateTimeOffset now, TimeSpan duration, int max, int maxInFlight)
    {
        var hash = StorageIdentity.Hash(trigger.Value);
        try
        {
            await EnsureClaimFence(hash);
            await using var scope = await open();
            return await InRetriableTransaction(scope.DbContext, async (context, transaction) =>
            {
                // The shared write serializes claims for different occurrence rows as well as the same row.
                await context.Set<OccurrenceClaimFence>().Where(row => row.Id == hash).ExecuteUpdateAsync(set => set.SetProperty(row => row.Version, row => row.Version + 1));
                var rows = await context.Set<OccurrenceEntry>().AsNoTracking()
                    .Where(row => row.Trigger == hash && (row.State == OccurrenceState.Pending || row.State == OccurrenceState.Leased)).ToListAsync();
                var byId = rows.ToDictionary(row => row.Id, StringComparer.Ordinal);
                var candidates = OccurrenceClaimSelection.Select(rows.Select(row => row.ToOccurrence()).ToArray(), now, max, maxInFlight)
                    .Select(occurrence => byId[StorageIdentity.Hash(occurrence.Id.Value)]).ToArray();
                var expiry = now.Add(duration).UtcTicks;
                var ticks = now.UtcTicks;
                var claimed = new List<Occurrence>();
                foreach (var row in candidates)
                {
                    if (await TryClaim(context, row, worker, ticks, expiry))
                    {
                        row.State = OccurrenceState.Leased;
                        row.Epoch++;
                        row.Attempts++;
                        row.Owner = worker;
                        row.Expiry = expiry;
                        row.NextAttempt = null;
                        claimed.Add(row.ToOccurrence());
                    }
                }

                await transaction.CommitAsync();
                return claimed.ToArray();
            });
        }
        catch (Exception error) when (StorageWriteConflicts.IsTransactionConflict(error))
        {
            // The transaction has been disposed/rolled back. The caller may claim again.
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Renew(LeaseToken token, DateTimeOffset expiry)
    {
        await using var scope = await open();
        var changed = await Leased(scope.DbContext, token).ExecuteUpdateAsync(set => set.SetProperty(row => row.Expiry, expiry.UtcTicks));
        return changed == 1 ? OccurrenceLeaseResult.Updated : OccurrenceLeaseResult.OccurrenceLeaseLost;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Complete(LeaseToken token)
    {
        await using var scope = await open();
        var changed = await Leased(scope.DbContext, token).ExecuteUpdateAsync(set => set.SetProperty(row => row.State, OccurrenceState.Completed).SetProperty(row => row.Owner, (string?)null).SetProperty(row => row.Expiry, (long?)null));
        if (changed == 1)
        {
            return OccurrenceLeaseResult.Updated;
        }

        var existing = await Get(token.OccurrenceId);
        return existing?.State == OccurrenceState.Completed && existing.Epoch == token.Epoch
            ? OccurrenceLeaseResult.AlreadyCompleted : OccurrenceLeaseResult.OccurrenceLeaseLost;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error, DateTimeOffset? nextAttempt)
    {
        await using var scope = await open();
        var state = nextAttempt.HasValue ? OccurrenceState.Pending : OccurrenceState.Quarantined;
        var ticks = nextAttempt?.UtcTicks;
        var changed = await Leased(scope.DbContext, token).ExecuteUpdateAsync(set => set.SetProperty(row => row.State, state).SetProperty(row => row.Owner, (string?)null).SetProperty(row => row.Expiry, (long?)null).SetProperty(row => row.LastError, error).SetProperty(row => row.NextAttempt, ticks));
        return changed == 1 ? OccurrenceLeaseResult.Updated : OccurrenceLeaseResult.OccurrenceLeaseLost;
    }

    /// <inheritdoc/>
    public async Task<bool> RetryQuarantined(OccurrenceId id)
    {
        await using var scope = await open();
        var hash = StorageIdentity.Hash(id.Value);
        return await scope.DbContext.Set<OccurrenceEntry>().Where(row => row.Id == hash && row.State == OccurrenceState.Quarantined)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.State, OccurrenceState.Pending).SetProperty(row => row.Attempts, 0).SetProperty(row => row.NextAttempt, (long?)null)) == 1;
    }

    static async Task<bool> TryClaim(TContext context, OccurrenceEntry observed, string worker, long now, long expiry)
    {
        var epoch = observed.Epoch;
        var state = observed.State;
        var query = context.Set<OccurrenceEntry>().Where(current => current.Id == observed.Id && current.Epoch == epoch && current.State == state);
        query = state == OccurrenceState.Pending
            ? query.Where(current => current.NextAttempt == null || current.NextAttempt <= now)
            : query.Where(current => current.Expiry <= now);
        return await query.ExecuteUpdateAsync(set => set.SetProperty(current => current.State, OccurrenceState.Leased).SetProperty(current => current.Epoch, current => current.Epoch + 1)
            .SetProperty(current => current.Attempts, current => current.Attempts + 1).SetProperty(current => current.Owner, worker).SetProperty(current => current.Expiry, expiry).SetProperty(current => current.NextAttempt, (long?)null)) == 1;
    }

    static IQueryable<OccurrenceEntry> Leased(TContext context, LeaseToken token)
    {
        var hash = StorageIdentity.Hash(token.OccurrenceId.Value);
        return context.Set<OccurrenceEntry>().Where(row => row.Id == hash && row.Epoch == token.Epoch && row.State == OccurrenceState.Leased);
    }

    /// <summary>
    /// Runs a user-initiated transaction as one retriable unit of the context's execution strategy.
    /// </summary>
    /// <remarks>
    /// A retrying strategy, such as the one PostgreSQL connections are configured with, refuses commands in a
    /// transaction it did not start. Each attempt starts from a clean change tracker because a failed attempt
    /// has rolled back everything it staged.
    /// </remarks>
    /// <param name="context">The context to run the transaction on.</param>
    /// <param name="work">The transactional work; it commits the transaction itself when it should take effect.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>The result of the attempt that completed.</returns>
    static Task<TResult> InRetriableTransaction<TResult>(TContext context, Func<TContext, IDbContextTransaction, Task<TResult>> work) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            return await work(context, transaction);
        });

    async Task EnsureClaimFence(string hash)
    {
        try
        {
            await using var scope = await open();
            if (await scope.DbContext.Set<OccurrenceClaimFence>().AnyAsync(row => row.Id == hash))
            {
                return;
            }

            scope.DbContext.Set<OccurrenceClaimFence>().Add(new OccurrenceClaimFence { Id = hash });
            await scope.DbContext.SaveChangesAsync();
        }
        catch (Exception error) when (StorageWriteConflicts.IsUnique(error))
        {
            await using var scope = await open();
            if (!await scope.DbContext.Set<OccurrenceClaimFence>().AnyAsync(row => row.Id == hash))
            {
                throw;
            }

            SqlStorageTelemetry.RecoveredCollision("occurrence-claim-fence");
        }
    }
}
