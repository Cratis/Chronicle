// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Records SQL coordination, recovered concurrent writes and failed live-query polls without identifying individual rows or databases.
/// </summary>
internal sealed class SqlStorageTelemetry
{
    static readonly Lazy<SqlStorageTelemetry> _legacy = new(() => new(MetricNames.Legacy));
    static readonly SqlStorageErrorClassifier _errors = new();
    readonly SqlStorageMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlStorageTelemetry"/> class.
    /// </summary>
    /// <param name="names">The selected naming mode.</param>
    internal SqlStorageTelemetry(MetricNames names) => _metrics = new(names);

    /// <summary>Gets the legacy instruments for callers without a configured database.</summary>
    internal static SqlStorageTelemetry Legacy => _legacy.Value;

    /// <summary>Gets the instruments selected for a database at producer construction.</summary>
    /// <param name="database">The configured database, if any.</param>
    /// <returns>The database's instruments, or the legacy defaults.</returns>
    internal static SqlStorageTelemetry For(IDatabase? database) => database is Database sql ? sql.Telemetry : Legacy;

    /// <summary>
    /// Measures just the database lock acquisition, including failed waits.
    /// </summary>
    /// <param name="acquire">Acquires the lock.</param>
    /// <param name="provider">The bounded EF provider name.</param>
    /// <returns>The acquisition task.</returns>
    internal async Task WaitForSchemaLock(Func<Task> acquire, string provider)
    {
        var started = Stopwatch.GetTimestamp();
        var outcome = "failed";
        try
        {
            await acquire();
            outcome = "acquired";
        }
        finally
        {
            _metrics.SchemaLockWait(Stopwatch.GetElapsedTime(started).TotalSeconds, provider, outcome);
        }
    }

    /// <summary>
    /// Counts a failed live-query poll that will be retried.
    /// </summary>
    /// <param name="query">The bounded query name.</param>
    /// <param name="error">The poll failure.</param>
    internal void LiveQueryPollFailed(string query, Exception error) => _metrics.LiveQueryPollFailed(query, _errors.IsTransient(error) ? "transient" : "other");

    /// <summary>
    /// Counts a concurrent insert that an existing recovery path handled.
    /// </summary>
    /// <param name="operation">The bounded storage operation name.</param>
    /// <param name="count">The number of collision retries recovered by the operation.</param>
    internal void RecoveredCollision(string operation, int count = 1) => _metrics.UpsertCollisionRecovered(operation, count);
}
