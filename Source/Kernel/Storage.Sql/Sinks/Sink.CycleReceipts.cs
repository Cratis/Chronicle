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
/// Residual risk, accepted: a duplicate application needs one open retry transaction to stay open for more than
/// 24 h (<see cref="OrphanRetention"/>) between its window check and its receipt insert, while the original commit
/// landed after that check and was swept. The providers make that practically impossible: the default command timeout
/// is 30 s for Npgsql, SqlClient and Microsoft.Data.Sqlite, the execution strategies give up after at most 6 retries
/// with a delay of at most 30 s each, and a server drops transactions whose connection is gone.
/// </remarks>
public partial class Sink
{
    static readonly long _sweepIntervalTicks = (long)TimeSpan.FromSeconds(60).TotalMilliseconds;
    int _receiptTableEnsured;
    long _lastSweep = long.MinValue;

    /// <summary>
    /// Gets or sets how long a call may be retried against its receipt, measured on the database clock. Settable so that specs need not wait.
    /// </summary>
    internal TimeSpan ReceiptVerificationWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets how long an orphaned receipt is kept, measured on the database clock. Settable so that specs need not wait.
    /// </summary>
    internal TimeSpan OrphanRetention { get; set; } = TimeSpan.FromHours(24);

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

    static bool IsMissingReceiptTable(Exception exception)
    {
        for (var current = (Exception?)exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(WellKnownTableNames.SinkCycleReceipts, StringComparison.Ordinal) &&
                (current.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase) ||
                 current.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                 current.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsReceiptCollision(DbUpdateException exception)
    {
        for (var current = (Exception?)exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(WellKnownTableNames.SinkCycleReceipts, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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

    /// <summary>
    /// The first statement of a retry attempt, in that attempt's transaction: one statement answers both whether the
    /// call is still inside its verification window, by database time, and whether its receipt is already there.
    /// </summary>
    /// <param name="context">The context whose transaction the statement joins.</param>
    /// <param name="receiptId">The call's receipt id.</param>
    /// <param name="startedAt">The database time the call started, when known.</param>
    /// <param name="sequenceNumber">The stored sequence number, for diagnostics.</param>
    /// <returns>True when the receipt exists, meaning the cycle already committed.</returns>
    /// <exception cref="SinkCycleOutcomeUnknown">Thrown when the call is past the verification window and has no receipt.</exception>
    async Task<bool> ProbeReceipt(ReadModelDbContext context, Guid receiptId, long? startedAt, long sequenceNumber)
    {
        var databaseType = context.Database.GetDatabaseType();
        var quote = databaseType == DatabaseType.SqlServer ? ("[", "]") : ("\"", "\"");
        var table = $"{quote.Item1}{WellKnownTableNames.SinkCycleReceipts}{quote.Item2}";
        var id = $"{quote.Item1}Id{quote.Item2}";
        var now = SinkCycleReceipt.NowSql(databaseType);
        var hasStart = startedAt is null ? 0 : 1;
        var started = startedAt ?? 0;

        // Found is reported as -1, otherwise the elapsed milliseconds since the call started, by the database clock.
        var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {table} WHERE {id} = {{0}}) THEN CAST(-1 AS bigint) WHEN {{1}} = 0 THEN CAST(0 AS bigint) ELSE {now} - {{2}} END AS \"Value\"";
#pragma warning disable EF1002 // Identifiers and the clock expression are constants of this assembly; the values are parameters.
        var result = await context.Database.SqlQueryRaw<long>(sql, receiptId, hasStart, started).SingleAsync();
#pragma warning restore EF1002
        if (result < 0)
        {
            return true;
        }

        if (TimeSpan.FromMilliseconds(result) > ReceiptVerificationWindow)
        {
            _logger.CycleOutcomeUnknown(_tableName, sequenceNumber, receiptId);
            throw new SinkCycleOutcomeUnknown($"The outcome of the multi-write cycle for sequence number {sequenceNumber} on '{_tableName}' (receipt {receiptId}) is unknown and too old to verify. Decide on a replay of the read model rather than applying it again.");
        }

        return false;
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
                var oldest = await DatabaseNow(context) - (long)OrphanRetention.TotalMilliseconds;
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
