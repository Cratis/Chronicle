// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class SqlCommandCapture : IDbConnectionInterceptor
{
    readonly ConcurrentQueue<Operation> _operations = new();

    /// <summary>
    /// Gets the asynchronous EF operation ledger, not a count of network trips. Dispatch boundaries do not prove
    /// sends, readers may still be streaming, pooled opens need not authenticate, and provider BEGIN may be deferred.
    /// Wire/server evidence is required before reporting network trips (including COMMIT and cleanup).
    /// </summary>
    public IReadOnlyList<Operation> Operations => _operations.ToArray();

    /// <summary>
    /// Counts individual INSERT or UPDATE statements for a specific table, even in a mixed receipt/row batch.
    /// </summary>
    /// <param name="sql">The command text.</param>
    /// <param name="verb">INSERT INTO or UPDATE.</param>
    /// <param name="table">The target table.</param>
    /// <returns>The statement count.</returns>
    public static int CountStatements(string sql, string verb, string table)
    {
        var unquoted = sql.Replace("\"", string.Empty, StringComparison.Ordinal).Replace("[", string.Empty, StringComparison.Ordinal).Replace("]", string.Empty, StringComparison.Ordinal);
        var target = Regex.Escape(table);
        var pattern = $@"\b{verb}\s+{target}\b";
        if (verb == "UPDATE")
        {
            pattern += $@"|\bUPDATE\s+\w+\s+SET\b[^;]*\bFROM\s+{target}\b";
        }

        return Regex.Count(unquoted, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    /// <inheritdoc/>
    public Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        RecordCommand("command.failed", command, eventData);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        RecordTransaction("rollback.requested", eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        RecordTransaction("transaction.failed", eventData);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        RecordConnection("connection.open-requested", eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        RecordConnection("connection.opened-not-authentication-proof", eventData);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        RecordConnection("connection.close-requested", eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
    {
        RecordConnection("connection.closed", eventData);
        return Task.CompletedTask;
    }

    void RecordCommand(string phase, DbCommand command, CommandEventData data) =>
        _operations.Enqueue(new(Stopwatch.GetTimestamp(), phase, Started, data.ConnectionId, data.Context?.Database.CurrentTransaction?.TransactionId, data.CommandId, command.CommandText));

    void RecordTransaction(string phase, TransactionEventData data) =>
        _operations.Enqueue(new(Stopwatch.GetTimestamp(), phase, Started, data.ConnectionId, data.TransactionId, null, null));

    void RecordConnection(string phase, ConnectionEventData data) =>
        _operations.Enqueue(new(Stopwatch.GetTimestamp(), phase, Started, data.ConnectionId, null, null, null));

    /// <summary>
    /// An EF boundary with monotonic timing and correlation IDs. Attempt is the observed transaction-start ordinal,
    /// not a claim that an implicit or deferred BEGIN made a network trip.
    /// </summary>
    /// <param name="Timestamp">The monotonic timestamp.</param>
    /// <param name="Phase">The observed boundary.</param>
    /// <param name="Attempt">The observed transaction ordinal.</param>
    /// <param name="ConnectionId">The EF connection ID.</param>
    /// <param name="TransactionId">The EF transaction ID, when available.</param>
    /// <param name="CommandId">The EF command ID, when available.</param>
    /// <param name="Sql">The SQL, for command boundaries.</param>
    public sealed record Operation(long Timestamp, string Phase, int Attempt, Guid ConnectionId, Guid? TransactionId, Guid? CommandId, string? Sql);
}
