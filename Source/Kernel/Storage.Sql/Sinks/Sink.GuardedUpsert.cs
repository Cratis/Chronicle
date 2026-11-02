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

    async Task<IEnumerable<FailedPartition>> ApplyGuardedUpsert(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber sequenceNumber)
    {
        var id = GetIdString(key);
        var incoming = checked((long)sequenceNumber.Value);
        var segments = OrderedSegmentsFromFirstDirectChange(changeset);
        var changes = DirectChangesOf(segments);
        var trailingJoins = segments.SelectMany(segment => segment).OfType<Joined>().ToArray();
        var joinsApplied = false;
        while (true)
        {
            await using var scope = await OpenActiveScope();
            var rows = scope.DbContext.Entries.Where(BuildIdPredicate(id));
            if (changeset.HasBeenRemoved())
            {
                await rows.Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == null || EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) < incoming).ExecuteDeleteAsync();
                return _noFailedPartitions;
            }
            if (!joinsApplied)
            {
                await ApplyJoinsAndChildRemovals(scope, changeset, sequenceNumber, joined => !trailingJoins.Any(trailing => ReferenceEquals(trailing, joined)));
                joinsApplied = true;
            }
            if (changes.Length == 0 || !await ShouldWriteDirectChanges(scope, id, changeset, changes))
            {
                await ApplyRemainingSegments(scope, new SegmentWrite(key, id, sequenceNumber, false), segments.Skip(1));
                return _noFailedPartitions;
            }

            var outcome = await TryApplyGuardedChanges(scope, key, new PendingWrite(id, segments[0], sequenceNumber));
            if (outcome != GuardedOutcome.Retry)
            {
                await ApplyRemainingSegments(scope, new SegmentWrite(key, id, sequenceNumber, outcome == GuardedOutcome.Applied), segments.Skip(1));
                return _noFailedPartitions;
            }
        }
    }

    async Task<GuardedOutcome> TryApplyGuardedChanges(DbContextScope<ReadModelDbContext> scope, Key key, PendingWrite write)
    {
        var entry = await GetOrAttachEntity(scope, write.Id, key);
        var original = ReadLastHandledSequenceNumber(entry);
        if (entry.State != EntityState.Added && original >= checked((long)write.SequenceNumber.Value))
        {
            return GuardedOutcome.Skipped;
        }
        ApplyChangesToEntity(entry, write.Changes);
        AdvanceLastHandledSequenceNumber(entry, write.SequenceNumber);
        if (entry.State == EntityState.Added)
        {
            return await TryInsertIsolated(scope.DbContext, write.Id) ? GuardedOutcome.Applied : GuardedOutcome.Retry;
        }

        // Compare the value read, not merely < incoming: a concurrent earlier event must be
        // incorporated by recomputing the changes, rather than overwritten with stale state.
        var desired = entry.Entity;
        scope.DbContext.ChangeTracker.Clear();
        var updated = await scope.DbContext.Entries.Where(BuildIdPredicate(write.Id))
            .Where(row => EF.Property<long?>(row, WellKnownProperties.LastHandledEventSequenceNumber) == original)
            .ExecuteUpdateAsync(setters => SetReplacementValues(setters, desired));
        return updated > 0 ? GuardedOutcome.Applied : GuardedOutcome.Retry;
    }
}
