// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that keeps the receipts of multi-write cycles.
/// </summary>
/// <remarks>
/// Every cycle whose execution strategy retries inserts a receipt on its first attempt, batched with its first write
/// in the same transaction. That attempt skips the probe but pays for the write, index maintenance and amortized cleanup.
/// SQLite's default non-retrying strategy writes no receipt. A cycle that finds its commit acknowledgment lost looks
/// for its receipt to learn whether the cycle already committed. Only the database
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
    readonly ConcurrentQueue<Guid> _finishedReceipts = new();
    int _receiptTableEnsured;

    /// <summary>
    /// Verify's own admission, separate from Migrate's flag so that neither mode can satisfy the other.
    /// </summary>
    int _receiptSchemaVerified;
    long _lastSweep = long.MinValue;

    /// <summary>
    /// Gets or sets how long a call may be retried against its receipt, measured on the database clock. Settable so that specs need not wait.
    /// </summary>
    internal TimeSpan ReceiptVerificationWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets how many finished receipts are deleted together. Settable so that specs can see a batch fill.
    /// </summary>
    internal int ReceiptCleanupBatchSize { get; set; } = 64;

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

    static void ClearReadModelTracking(ReadModelDbContext context)
    {
        // Outside a cycle there is no receipt to keep, so a plain clear is all it takes.
        if (context.Database.CurrentTransaction is null)
        {
            context.ChangeTracker.Clear();
            return;
        }

        // An unsaved receipt must ride on the first real save, even when a guarded CAS clears row state.
        var pending = context.ChangeTracker.Entries<SinkCycleReceipt>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToArray();
        context.ChangeTracker.Clear();
        context.SinkCycleReceipts.AddRange(pending);
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

    /// <summary>
    /// In Verify, admits the cycle after one read-only validation per sink. It never creates or repairs anything.
    /// </summary>
    /// <param name="cycleContext">The cycle's own context, whose options decide the schema mode.</param>
    /// <returns>Awaitable task.</returns>
    async Task EnsureCycleReceiptTable(ReadModelDbContext cycleContext)
    {
        if (!SinkCycleReceiptSchema.IsVerify(cycleContext))
        {
            await EnsureCycleReceiptTable();
            return;
        }

        if (Volatile.Read(ref _receiptSchemaVerified) == 1)
        {
            return;
        }

        await SinkCycleReceiptSchema.Validate(cycleContext);
        Volatile.Write(ref _receiptSchemaVerified, 1);
    }

    /// <summary>
    /// Handles a missing receipt table in Verify: forgets the positive result, reports what is missing
    /// and leaves the original error to the caller. Nothing is created, repaired or retried.
    /// </summary>
    /// <param name="context">The cycle's context.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="SchemaNotCurrent">The receipt schema is missing or drifted.</exception>
    async Task RejectVerifyDrift(ReadModelDbContext context)
    {
        Volatile.Write(ref _receiptSchemaVerified, 0);
        await SinkCycleReceiptSchema.Validate(context);
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

    /// <summary>
    /// Queues the finished call's receipt for deletion. The queue is deleted in one statement once it holds
    /// <see cref="ReceiptCleanupBatchSize"/> receipts, or together with the orphan sweep when that is due.
    /// </summary>
    /// <param name="context">The cycle's context.</param>
    /// <param name="receiptId">The finished call's receipt id.</param>
    /// <returns>Awaitable task.</returns>
    async Task CleanUpReceipts(ReadModelDbContext context, Guid receiptId)
    {
        _finishedReceipts.Enqueue(receiptId);
        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _lastSweep);
        var sweep = (last == long.MinValue || now - last >= _sweepIntervalTicks) && Interlocked.CompareExchange(ref _lastSweep, now, last) == last;
        if (!sweep && _finishedReceipts.Count < ReceiptCleanupBatchSize)
        {
            return;
        }

        try
        {
            context.ChangeTracker.Clear();
            var finished = new List<Guid>(ReceiptCleanupBatchSize);
            while (finished.Count < ReceiptCleanupBatchSize && _finishedReceipts.TryDequeue(out var queued))
            {
                finished.Add(queued);
            }

            await DeleteReceipts(context, finished, sweep);
        }
        catch (Exception exception)
        {
            // Best effort: the sweep removes whatever this call leaves behind.
            _logger.CycleReceiptCleanupFailed(exception, _tableName);
        }
    }

    async Task DeleteReceipts(ReadModelDbContext context, List<Guid> finished, bool sweep)
    {
        // Only scheduling uses process time. Expiry is evaluated once on the database, with owner cleanup.
        var databaseType = context.Database.GetDatabaseType();
        var quote = databaseType == DatabaseType.SqlServer ? ("[", "]") : ("\"", "\"");
        var table = $"{quote.Item1}{WellKnownTableNames.SinkCycleReceipts}{quote.Item2}";
        var id = $"{quote.Item1}Id{quote.Item2}";
        var createdAt = $"{quote.Item1}CreatedAt{quote.Item2}";
        var parameters = new List<object>(finished.Count + 1);
        parameters.AddRange(finished.Cast<object>());
        var conditions = new List<string>(2);
        if (finished.Count > 0)
        {
            conditions.Add($"{id} IN ({string.Join(", ", finished.Select((_, index) => $"{{{index}}}"))})");
        }

        if (sweep)
        {
            conditions.Add($"{createdAt} < {SinkCycleReceipt.NowSql(databaseType)} - {{{parameters.Count}}}");
            parameters.Add((long)OrphanRetention.TotalMilliseconds);
        }

        var sql = $"DELETE FROM {table} WHERE {string.Join(" OR ", conditions)}";
#pragma warning disable EF1002 // Identifiers and clock are assembly constants; values are parameters.
        await context.Database.ExecuteSqlRawAsync(sql, parameters);
#pragma warning restore EF1002
    }
}
