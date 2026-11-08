// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Drops fixture-owned databases and waits for MongoDB to accept collection creation again.
/// </summary>
internal static class MongoDBDatabaseDropper
{
    const int DatabaseDropPending = 215;
    const int DatabaseDropAlreadyInProgress = 10055;
    static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);
    static readonly TimeSpan _retryInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Drops a database and removes a readiness probe before returning.
    /// </summary>
    /// <param name="client">The MongoDB client.</param>
    /// <param name="name">The database name.</param>
    /// <returns>A task representing the drop and readiness check.</returns>
    internal static Task DropAndWait(IMongoClient client, string name)
    {
        var clock = Stopwatch.StartNew();
        return DropAndWait(client, name, new SettleTiming(_defaultTimeout, () => clock.Elapsed, Task.Delay));
    }

    /// <summary>
    /// Drops a database and checks readiness using the supplied timing policy.
    /// </summary>
    /// <param name="client">The MongoDB client.</param>
    /// <param name="name">The database name.</param>
    /// <param name="timing">The shared deadline, clock and backoff.</param>
    /// <returns>A task representing the drop and readiness check.</returns>
    internal static async Task DropAndWait(IMongoClient client, string name, SettleTiming timing)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timing.Timeout, TimeSpan.Zero);
        using var phase = IntegrationTimings.BeginCurrent("mongodb-drop-settle");
        var deadline = new DropDeadline(name, timing.Timeout, timing.Elapsed, timing.Delay);
        await Retry(() => client.DropDatabaseAsync(name), IsDropInProgress, deadline, "drop");

        var database = client.GetDatabase(name);
        var probe = $"__drop_settle_{Guid.NewGuid():N}";
        await Retry(
            () =>
            {
                phase?.Poll();
                return database.CreateCollectionAsync(probe);
            },
            IsDatabaseDropPending,
            deadline,
            "probe");

        // Removing only our collection must not start another database drop.
        await database.DropCollectionAsync(probe);
        deadline.EnsureTime("probe cleanup");
        phase?.Success();
    }

    static async Task Retry(Func<Task> command, Func<MongoException, bool> isTransient, DropDeadline deadline, string stage)
    {
        while (true)
        {
            deadline.EnsureTime(stage);
            try
            {
                await command();
                return;
            }
            catch (MongoException exception) when (isTransient(exception))
            {
                IntegrationTimings.CurrentPhase?.Retry();
                await deadline.Backoff(stage, exception);
            }
        }
    }

    static bool IsDropInProgress(MongoException exception) =>
        exception is MongoCommandException { Code: DatabaseDropAlreadyInProgress or DatabaseDropPending } ||
        exception.Message.Contains("currently being dropped", StringComparison.OrdinalIgnoreCase);

    static bool IsDatabaseDropPending(MongoException exception) =>
        exception is MongoCommandException { Code: DatabaseDropPending } ||
        exception.Message.Contains("in the process of being dropped", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Supplies the settle budget, monotonic clock and retry delay.
    /// </summary>
    /// <param name="Timeout">The overall deadline budget.</param>
    /// <param name="Elapsed">The elapsed time since the drop began.</param>
    /// <param name="Delay">The retry backoff.</param>
    internal sealed record SettleTiming(TimeSpan Timeout, Func<TimeSpan> Elapsed, Func<TimeSpan, Task> Delay);

    sealed class DropDeadline(string name, TimeSpan timeout, Func<TimeSpan> elapsed, Func<TimeSpan, Task> delay)
    {
        Exception? _lastTransient;

        internal void EnsureTime(string stage) => _ = RemainingTime(stage);

        internal Task Backoff(string stage, MongoException exception)
        {
            _lastTransient = exception;
            var remaining = RemainingTime(stage);
            return delay(remaining < _retryInterval ? remaining : _retryInterval);
        }

        TimeSpan RemainingTime(string stage)
        {
            var remaining = timeout - elapsed();
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException($"MongoDB database '{name}' did not settle during {stage} within {timeout}.", _lastTransient);
            }
            return remaining;
        }
    }
}
