// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that runs a multi-write cycle in one database transaction.
/// </summary>
/// <remarks>
/// A cycle with a single write keeps its single command and its own implicit transaction. A cycle with two or more
/// writes (joins, child removals and the direct change) is rerun as a whole when it fails and nothing else is kep
/// across attempts, so every provider behaves the same: no savepoints, and an aborted PostgreSQL transaction is never
/// reused. Whole-cycle rerun is also how an insert race, a lost guarded compare-and-swap and a missing shadow column
/// are recovered. <see cref="Sink"/> stamps the composite key outside the transaction. Moving it inside is
/// #5427's work. This is partial groundwork for #5427.
/// </remarks>
public partial class Sink
{
    const int MaxInsertRaceReruns = 5;

    /// <summary>
    /// Determines whether the changes need two or more writes. Joins and child removals count one step each and every other
    /// change together counts as one direct step. It allocates nothing for the list and array shapes a changeset has.
    /// </summary>
    /// <param name="changes">The changes of the changeset.</param>
    /// <returns>True when the cycle has two or more write steps.</returns>
    internal static bool IsMultiWrite(IEnumerable<Change> changes) =>
        changes is IReadOnlyList<Change> list ? IsMultiWriteList(list) : IsMultiWriteEnumerated(changes);

    static bool IsMultiWriteList(IReadOnlyList<Change> changes)
    {
        var steps = default(WriteStepCount);
        for (var i = 0; i < changes.Count; i++)
        {
            if (steps.AddReachesTwo(changes[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Any other shape of changes is not what a changeset has, so this may allocate.
    /// </summary>
    /// <param name="changes">The changes.</param>
    /// <returns>True when the cycle has two or more write steps.</returns>
    static bool IsMultiWriteEnumerated(IEnumerable<Change> changes) =>
        changes.Count(change => change is Joined or ChildRemovedFromAll) + (changes.Any(change => change is not Joined and not ChildRemovedFromAll) ? 1 : 0) >= 2;

    static bool IsVerifyDrift(bool verifying, Exception exception) =>
        verifying && IsMissingReceiptTable(exception);

    async Task<IEnumerable<FailedPartition>> ApplyCycle(CycleWrite write)
    {
        var nonJoinedChanges = write.Changeset.Changes
            .Where(c => c is not Joined and not ChildRemovedFromAll)
            .ToArray();
        var wroteDirect = await RunInCycleTransaction(write.Scope, write.SequenceNumber, nonJoinedChanges.Length > 0, () => ApplyJoinsThenDirectChanges(write, nonJoinedChanges));
        if (wroteDirect)
        {
            await SqlCompositeKeyShadow.Stamp(write.Scope.DbContext, write.Key, write.Id, _logger);
        }

        return _noFailedPartitions;
    }

    async Task<bool> ApplyJoinsThenDirectChanges(CycleWrite write, Change[] nonJoinedChanges)
    {
        var scope = write.Scope;
        await ApplyJoinsAndChildRemovals(scope, write.Changeset, write.SequenceNumber, write.Key.ArrayIndexers);
        if (nonJoinedChanges.Length == 0 || !await ShouldWriteDirectChanges(scope, write.Id, write.Changeset, nonJoinedChanges))
        {
            return false;
        }

        var entry = await GetOrAttachEntity(scope, write.Id, write.Key);
        if (IsSkippedByWatermark(entry, write.Mode, write.SequenceNumber))
        {
            entry.State = EntityState.Detached;
            return false;
        }

        ApplyChangesToEntity(entry, nonJoinedChanges, write.Key.ArrayIndexers);
        AdvanceLastHandledSequenceNumber(entry, write.SequenceNumber);
        await SaveRecoveringFromInsertRace(scope, entry, new PendingWrite(write.Id, nonJoinedChanges, write.SequenceNumber, write.Key.ArrayIndexers));
        return true;
    }

    /// <summary>
    /// Runs the body of a cycle in a transaction together with the cycle's receipt.
    /// </summary>
    /// <param name="scope">The scope holding the context.</param>
    /// <param name="sequenceNumber">The sequence number applied.</param>
    /// <param name="hasDirectChanges">Whether the cycle has a direct change. When an earlier attempt of this call already committed, its direct change counts as written.</param>
    /// <param name="body">The writes. Returns whether the direct change was written.</param>
    /// <returns>Whether the direct change was written.</returns>
    async Task<bool> RunInCycleTransaction(DbContextScope<ReadModelDbContext> scope, EventSequenceNumber sequenceNumber, bool hasDirectChanges, Func<Task<bool>> body)
    {
        var context = scope.DbContext;
        await EnsureCycleReceiptTable(context);
        var verifying = SinkCycleReceiptSchema.IsVerify(context);

        // The whole cycle is rerun instead of rolling back to a savepoint, which behaves the same on every provider.
        context.Database.AutoSavepointsEnabled = false;
        var strategy = context.Database.CreateExecutionStrategy();

        // A receipt only protects a retry after a lost commit acknowledgment, so a strategy that never retries needs none.
        var state = new CycleAttemptState(Guid.CreateVersion7(), SinkCycleReceipt.ToStored(sequenceNumber.Value), strategy.RetriesOnFailure);
        var insertRaceReruns = 0;
        var repaired = false;
        var tableRepaired = false;
        try
        {
            while (true)
            {
                try
                {
                    await strategy.ExecuteInTransactionAsync(
                        () => RunCycleAttempt(context, state, body),

                        // The probe is the first statement of every retry attempt, in that attempt's transaction, so there is nothing to verify here.
                        () => Task.FromResult(false));
                    return state.WroteDirect;
                }
                catch (CycleRerun)
                {
                    // The transaction was rolled back; apply the whole cycle again on current state.
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception) && IsReceiptCollision(exception))
                {
                    // The receipt key is already taken: an earlier attempt of this call committed. Never rerun.
                    // That attempt wrote the direct change, so the idempotent composite-key stamp must still run.
                    return hasDirectChanges;
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception) && ++insertRaceReruns <= MaxInsertRaceReruns)
                {
                    // Another writer inserted the row first. Inside a transaction we do not recover in place:
                    // PostgreSQL has aborted it. Rerun, and the row is now found and updated.
                    SqlStorageTelemetry.RecoveredCollision("read-model");
                }
                catch (Exception exception) when (IsVerifyDrift(verifying, exception))
                {
                    // Verify never repairs. Report what is missing, or surface the original error when the receipts are fine.
                    await RejectVerifyDrift(context);
                    throw;
                }
                catch (Exception exception) when (!tableRepaired && IsMissingReceiptTable(exception))
                {
                    // The database lost the table after this sink created it, such as after a restore.
                    tableRepaired = true;
                    Volatile.Write(ref _receiptTableEnsured, 0);
                    await EnsureCycleReceiptTable();
                }
                catch (Exception exception) when (!repaired && ShadowColumns.IsMissingColumn(exception))
                {
                    repaired = true;
                    await context.RepairShadowColumns();
                }
            }
        }
        finally
        {
            await CleanUpReceiptsWhenUsed(context, state);
        }
    }

    Task CleanUpReceiptsWhenUsed(ReadModelDbContext context, CycleAttemptState state) =>
        state.UsesReceipt ? CleanUpReceipts(context, state.ReceiptId) : Task.CompletedTask;

    async Task RunCycleAttempt(ReadModelDbContext context, CycleAttemptState state, Func<Task<bool>> body)
    {
        context.ChangeTracker.Clear();
        if (!state.UsesReceipt)
        {
            state.WroteDirect = await body();
            return;
        }

        await RunReceiptedCycleAttempt(context, state, body);
    }

    async Task RunReceiptedCycleAttempt(ReadModelDbContext context, CycleAttemptState state, Func<Task<bool>> body)
    {
        if (state.Attempts++ > 0 && await ProbeReceipt(context, state.ReceiptId, state.StartedAt, state.Sequence))
        {
            // A previous attempt committed and only its acknowledgment was lost.
            return;
        }

        state.WroteDirect = false;

        var receipt = new SinkCycleReceipt(state.ReceiptId, ActiveTableName, state.Sequence, 0);
        context.SinkCycleReceipts.Add(receipt);
        state.WroteDirect = await body();
        await FlushReceiptIfPending(context, receipt);
        state.StartedAt ??= receipt.CreatedAt;
    }

    /// <summary>
    /// Counts the write steps of a changeset. Joins and child removals count one step each and every other change together counts as one.
    /// </summary>
    struct WriteStepCount
    {
        int _steps;
        bool _direct;

        internal bool AddReachesTwo(Change change)
        {
            if (change is Joined or ChildRemovedFromAll)
            {
                _steps++;
            }
            else if (!_direct)
            {
                _direct = true;
                _steps++;
            }

            return _steps >= 2;
        }
    }

    /// <summary>
    /// The inputs of a multi-write cycle. It is created only for a multi-write cycle, so a single write allocates nothing for it.
    /// </summary>
    sealed record CycleWrite
    {
        internal required DbContextScope<ReadModelDbContext> Scope { get; init; }

        internal required Key Key { get; init; }

        internal required string Id { get; init; }

        internal required IChangeset<AppendedEvent, ExpandoObject> Changeset { get; init; }

        internal required EventSequenceNumber SequenceNumber { get; init; }

        internal required SinkWriteMode Mode { get; init; }
    }

    sealed class CycleAttemptState(Guid receiptId, long sequence, bool usesReceipt)
    {
        internal bool UsesReceipt { get; } = usesReceipt;

        internal Guid ReceiptId { get; } = receiptId;

        internal long Sequence { get; } = sequence;

        internal long? StartedAt { get; set; }

        internal int Attempts { get; set; }

        internal bool WroteDirect { get; set; }
    }

#pragma warning disable CA1064 // Private control-flow signal that never leaves this class.
    sealed class CycleRerun : Exception;
#pragma warning restore CA1064
}
