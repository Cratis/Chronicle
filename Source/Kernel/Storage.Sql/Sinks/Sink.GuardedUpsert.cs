// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    enum GuardedOutcome
    {
        Retry = 0,
        Skipped = 1,
        Applied = 2
    }

    static bool IsGuardedCycle(IChangeset<AppendedEvent, ExpandoObject> changeset) =>
        IsMultiWrite(changeset.Changes) && !changeset.HasBeenRemoved();

    async Task<IEnumerable<FailedPartition>> ApplyGuardedUpsert(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber sequenceNumber)
    {
        var id = GetIdString(key);
        var incoming = checked((long)sequenceNumber.Value);
        var changes = changeset.Changes.Where(change => change is not Joined and not ChildRemovedFromAll).ToArray();
        if (IsGuardedCycle(changeset))
        {
            return await ApplyGuardedCycle(key, changeset, new PendingWrite(id, changes, sequenceNumber, key.ArrayIndexers));
        }

        var joinsApplied = false;
        while (true)
        {
            await using var scope = await OpenActiveScope();
            if (changeset.HasBeenRemoved())
            {
                await DeleteRowsNotNewerThan(scope, id, incoming);
                return _noFailedPartitions;
            }
            if (!joinsApplied)
            {
                await ApplyJoinsAndChildRemovals(scope, changeset.Changes, sequenceNumber, key.ArrayIndexers);
                joinsApplied = true;
            }
            if (changes.Length == 0 || !await ShouldWriteDirectChanges(scope, id, changeset, changes))
            {
                return _noFailedPartitions;
            }

            if (await TryApplyGuardedChanges(scope, key, new PendingWrite(id, changes, sequenceNumber, key.ArrayIndexers)) != GuardedOutcome.Retry)
            {
                return _noFailedPartitions;
            }
        }
    }

    async Task<IEnumerable<FailedPartition>> ApplyGuardedCycle(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, PendingWrite write)
    {
        await using var cycleScope = await OpenActiveScope();
        await RunInCycleTransaction(cycleScope, write.SequenceNumber, write.Changes.Length > 0, async () =>
        {
            var order = OrderWrites(changeset.Changes);
            await ApplyJoinsAndChildRemovals(cycleScope, order.Leading, write.SequenceNumber, key.ArrayIndexers);
            var wroteDirect = false;
            var directApplied = false;
            if (write.Changes.Length > 0 && await ShouldWriteDirectChanges(cycleScope, write.Id, changeset, write.Changes))
            {
                // A lost compare-and-swap reruns the whole cycle, so joins are applied once, on current state.
                var outcome = await TryApplyGuardedChanges(cycleScope, key, write with { Changes = order.FirstDirect });
                wroteDirect = outcome != GuardedOutcome.Retry ? true : throw new CycleRerun();
                directApplied = outcome == GuardedOutcome.Applied;
            }

            await ApplyTrailingRuns(cycleScope, key, write.Id, write.SequenceNumber, order.Trailing, directApplied);
            return wroteDirect;
        });
        return _noFailedPartitions;
    }

    Task<int> DeleteRowsNotNewerThan(DbContextScope<ReadModelDbContext> scope, string id, long incoming) =>
        scope.DbContext.Entries.Where(BuildIdPredicate(id))
            .Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == null || EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) < incoming)
            .ExecuteDeleteAsync();

    async Task<GuardedOutcome> TryApplyGuardedChanges(DbContextScope<ReadModelDbContext> scope, Key key, PendingWrite write)
    {
        var entry = await GetOrAttachEntity(scope, write.Id, key);
        var original = ReadLastHandledSequenceNumber(entry);
        if (entry.State != EntityState.Added && original >= checked((long)write.SequenceNumber.Value))
        {
            return GuardedOutcome.Skipped;
        }
        ApplyChangesToEntity(entry, write.Changes, write.ArrayIndexers);
        AdvanceLastHandledSequenceNumber(entry, write.SequenceNumber);
        if (entry.State == EntityState.Added)
        {
            return await TryInsertIsolated(scope.DbContext, write.Id) ? GuardedOutcome.Applied : GuardedOutcome.Retry;
        }

        // Compare the value read, not merely < incoming: a concurrent earlier event must be
        // incorporated by recomputing the changes, rather than overwritten with stale state.
        var desired = entry.Entity;
        ClearReadModelTracking(scope.DbContext);
        var updated = await scope.DbContext.Entries.Where(BuildIdPredicate(write.Id))
            .Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == original)
            .ExecuteUpdateAsync(setters => SetReplacementValues(setters, desired));
        return updated > 0 ? GuardedOutcome.Applied : GuardedOutcome.Retry;
    }
}
