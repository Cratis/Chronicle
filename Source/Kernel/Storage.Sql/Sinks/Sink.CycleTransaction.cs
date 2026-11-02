// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that runs a multi-write cycle in one database transaction.
/// </summary>
/// <remarks>
/// A cycle with a single write keeps its single command and its own implicit transaction. A cycle with two or more
/// writes (joins, child removals and the direct change) is rerun as a whole when it fails and nothing else is kept
/// across attempts, so every provider behaves the same: no savepoints, and an aborted PostgreSQL transaction is never
/// reused. Whole-cycle rerun is also how an insert race, a lost guarded compare-and-swap and a missing shadow column
/// are recovered. <see cref="Sink"/> stamps the composite key outside the transaction. Moving it inside is
/// #5427's work. This is partial groundwork for #5427.
/// </remarks>
public partial class Sink
{
    const int MaxInsertRaceReruns = 5;

    static int WriteSteps(IChangeset<AppendedEvent, ExpandoObject> changeset, Change[] nonJoinedChanges) =>
        changeset.Changes.Count(change => change is Joined or ChildRemovedFromAll) + (nonJoinedChanges.Length > 0 ? 1 : 0);

    /// <summary>
    /// Runs the body of a cycle in a transaction together with the cycle's receipt.
    /// </summary>
    /// <param name="scope">The scope holding the context.</param>
    /// <param name="sequenceNumber">The sequence number applied.</param>
    /// <param name="flushReceiptFirst">Whether to write the receipt before the body, for bodies that clear the change tracker.</param>
    /// <param name="body">The writes. Returns whether the direct change was written.</param>
    /// <returns>Whether the direct change was written.</returns>
    async Task<bool> RunInCycleTransaction(DbContextScope<ReadModelDbContext> scope, EventSequenceNumber sequenceNumber, bool flushReceiptFirst, Func<Task<bool>> body)
    {
        await EnsureCycleReceiptTable();
        var context = scope.DbContext;

        // The whole cycle is rerun instead of rolling back to a savepoint, which behaves the same on every provider.
        context.Database.AutoSavepointsEnabled = false;
        var strategy = context.Database.CreateExecutionStrategy();
        var receiptId = Guid.CreateVersion7();
        var sequence = checked((long)sequenceNumber.Value);
        long? startedAt = null;
        var attempts = 0;
        var insertRaceReruns = 0;
        var repaired = false;
        var wroteDirect = false;
        try
        {
            while (true)
            {
                try
                {
                    await strategy.ExecuteInTransactionAsync(
                        async () =>
                        {
                            context.ChangeTracker.Clear();
                            if (attempts++ > 0)
                            {
                                await FailClosedWhenTooOld(context, receiptId, startedAt, sequence);
                            }

                            var receipt = new SinkCycleReceipt(receiptId, ActiveTableName, sequence, 0);
                            context.SinkCycleReceipts.Add(receipt);
                            if (flushReceiptFirst)
                            {
                                await FlushReceiptIfPending(context, receipt);
                            }

                            wroteDirect = await body();
                            await FlushReceiptIfPending(context, receipt);
                            startedAt ??= receipt.CreatedAt;
                        },
                        () => ReceiptCommitted(context, receiptId, startedAt, sequence));
                    return wroteDirect;
                }
                catch (CycleRerun)
                {
                    // The transaction was rolled back; apply the whole cycle again on current state.
                }
                catch (DbUpdateException exception) when (IsUniqueViolation(exception) && ++insertRaceReruns <= MaxInsertRaceReruns)
                {
                    // Another writer inserted the row first. Inside a transaction we do not recover in place:
                    // PostgreSQL has aborted it. Rerun, and the row is now found and updated.
                    SqlStorageTelemetry.RecoveredCollision("read-model");
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
            await DeleteReceiptAndSweepOrphans(context, receiptId);
        }
    }

#pragma warning disable CA1064 // Private control-flow signal that never leaves this class.
    sealed class CycleRerun : Exception;
#pragma warning restore CA1064
}
