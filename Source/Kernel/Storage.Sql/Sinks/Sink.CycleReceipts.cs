// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that keeps the receipts of multi-write cycles.
/// </summary>
/// <remarks>
/// A receipt is inserted in the cycle's own transaction, batched with the cycle's first write. A cycle that finds
/// its commit acknowledgment lost looks for its receipt to learn whether the cycle already committed. Only the database
/// clock is used, so skewed silo clocks have no effect. The owning call deletes its receipt when it is done. Receipts of
/// crashed owners are swept after <see cref="OrphanRetention"/>.
/// A duplicate application needs one open retry transaction to pause for longer than <see cref="OrphanRetention"/>
/// between its verification window check and its receipt lookup, while the original commit landed after that check and
/// was swept. That needs a stalled transaction longer than any command timeout or connection lifetime in the
/// providers. It is accepted.
/// </remarks>
public partial class Sink
{
    static readonly TimeSpan _receiptVerificationWindow = TimeSpan.FromMinutes(5);
    static readonly TimeSpan _orphanRetention = TimeSpan.FromHours(24);
    static readonly long _sweepIntervalTicks = (long)TimeSpan.FromSeconds(60).TotalMilliseconds;
    int _receiptTableEnsured;
    long _lastSweep = long.MinValue;

    static TimeSpan OrphanRetention => _orphanRetention;

    static async Task FlushReceiptIfPending(ReadModelDbContext context, SinkCycleReceipt receipt)
    {
        if (context.Entry(receipt).State == EntityState.Added)
        {
            await context.SaveChangesAsync();
        }
    }

    static async Task<long> DatabaseNow(ReadModelDbContext context)
    {
        var sql = $"SELECT {SinkCycleReceipt.NowSql(context.Database.GetDatabaseType())} AS \"Value\"";
#pragma warning disable EF1002 // The expression is a constant of this assembly.
        return await context.Database.SqlQueryRaw<long>(sql).SingleAsync();
#pragma warning restore EF1002
    }

    async Task EnsureCycleReceiptTable()
    {
        if (Volatile.Read(ref _receiptTableEnsured) == 1)
        {
            return;
        }

        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, ActiveTableName, _columns, ConnectionPurpose.Schema);
        await scope.DbContext.EnsureSinkCycleReceipts();
        Volatile.Write(ref _receiptTableEnsured, 1);
    }

    async Task FailClosedWhenTooOld(ReadModelDbContext context, Guid receiptId, long? startedAt, long sequenceNumber)
    {
        if (startedAt is null)
        {
            return;
        }

        var elapsed = TimeSpan.FromMilliseconds(await DatabaseNow(context) - startedAt.Value);
        if (elapsed > _receiptVerificationWindow)
        {
            _logger.CycleOutcomeUnknown(_tableName, sequenceNumber, receiptId);
            throw new SinkCycleOutcomeUnknown($"The outcome of the multi-write cycle for sequence number {sequenceNumber} on '{_tableName}' (receipt {receiptId}) is unknown and too old to verify. Decide on a replay of the read model rather than applying it again.");
        }
    }

    async Task<bool> ReceiptCommitted(ReadModelDbContext context, Guid receiptId, long? startedAt, long sequenceNumber)
    {
        await FailClosedWhenTooOld(context, receiptId, startedAt, sequenceNumber);
        context.ChangeTracker.Clear();
        return await context.SinkCycleReceipts.AsNoTracking().AnyAsync(receipt => receipt.Id == receiptId);
    }

    async Task DeleteReceiptAndSweepOrphans(ReadModelDbContext context, Guid receiptId)
    {
        try
        {
            context.ChangeTracker.Clear();
            await context.SinkCycleReceipts.Where(receipt => receipt.Id == receiptId).ExecuteDeleteAsync();
            var now = Environment.TickCount64;
            var last = Interlocked.Read(ref _lastSweep);
            if ((last == long.MinValue || now - last >= _sweepIntervalTicks) && Interlocked.CompareExchange(ref _lastSweep, now, last) == last)
            {
                var oldest = await DatabaseNow(context) - (long)_orphanRetention.TotalMilliseconds;
                await context.SinkCycleReceipts.Where(receipt => receipt.CreatedAt < oldest).ExecuteDeleteAsync();
            }
        }
        catch (Exception exception)
        {
            // Best effort: the sweep removes whatever this call leaves behind.
            _logger.CycleReceiptCleanupFailed(exception, _tableName);
        }
    }
}
