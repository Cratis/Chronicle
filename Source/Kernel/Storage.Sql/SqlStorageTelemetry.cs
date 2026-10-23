// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Metrics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Records SQL coordination and recovered concurrent writes without identifying individual rows or databases.
/// </summary>
internal sealed class SqlStorageTelemetry
{
    static readonly IMeter<SqlStorageTelemetry> _meter = new Meter<SqlStorageTelemetry>(new System.Diagnostics.Metrics.Meter(WellKnown.MeterName));

    /// <summary>
    /// Measures just the database lock acquisition, including failed waits.
    /// </summary>
    /// <param name="acquire">Acquires the lock.</param>
    /// <param name="provider">The bounded EF provider name.</param>
    /// <returns>The acquisition task.</returns>
    internal static async Task WaitForSchemaLock(Func<Task> acquire, string provider)
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
            _meter.SchemaLockWait(Stopwatch.GetElapsedTime(started).TotalSeconds, provider, outcome);
        }
    }

    /// <summary>
    /// Counts a concurrent insert that an existing recovery path handled.
    /// </summary>
    /// <param name="operation">The bounded storage operation name.</param>
    /// <param name="count">The number of collision retries recovered by the operation.</param>
    internal static void RecoveredCollision(string operation, int count = 1) => _meter.UpsertCollisionRecovered(operation, count);
}
