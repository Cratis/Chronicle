// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Helpers for building live, database-watching observable queries for the SQL storage backends.
/// </summary>
/// <remarks>
/// MongoDB delivers live updates through collection change-streams. The SQL backends cannot use Arc's
/// push-based <c language="csharp">DbSet.Observe()</c> here because every event store and namespace lives in its own
/// database created on demand with a per-store connection string — those contexts are not registered
/// in dependency injection, which <c language="csharp">DbSet.Observe()</c> requires to build its listener. Instead this
/// polls the supplied query on an interval and re-emits only when the result actually changes, giving
/// subscribers automatic updates without a manual refresh while honoring the per-store connection.
/// Failed polls are logged, metered and retried with capped exponential backoff, never delivered as
/// <c language="csharp">OnError</c> to the subscriber.
/// </remarks>
public static class LiveQuery
{
    /// <summary>
    /// The default interval between polls of the underlying database.
    /// </summary>
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The longest delay between retries of a failing poll, unless the polling interval itself is longer.
    /// </summary>
    internal static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates an <see cref="ISubject{T}"/> that emits the current result of <paramref name="query"/> on
    /// subscription and re-emits whenever the result changes, polling the database on an interval.
    /// </summary>
    /// <param name="query">Reads the current state from the database.</param>
    /// <param name="pollingInterval">Optional interval between polls. Defaults to <see cref="DefaultPollingInterval"/>.</param>
    /// <typeparam name="TResult">Type of the items emitted to subscribers.</typeparam>
    /// <returns>
    /// An <see cref="ISubject{T}"/> delivering the initial snapshot and subsequent changes. The subject is
    /// read-only — its observer side is inert because the database is the source of truth; nothing pushes
    /// into it. Failed polls are logged, metered and retried with backoff, never delivered as
    /// <c language="csharp">OnError</c>. Polling stops when the subscriber unsubscribes.
    /// </returns>
    public static ISubject<IEnumerable<TResult>> Observe<TResult>(Func<Task<IEnumerable<TResult>>> query, TimeSpan? pollingInterval = null) =>
        Observe(query, pollingInterval, EqualityComparer<TResult>.Default);

    /// <summary>
    /// Observes a query using structural item equality to detect changes across snapshots.
    /// </summary>
    /// <param name="query">Reads the current state.</param>
    /// <param name="pollingInterval">The interval between polls.</param>
    /// <param name="comparer">The item equality comparer.</param>
    /// <typeparam name="TResult">The type of each item.</typeparam>
    /// <returns>
    /// A subject whose subscriptions stop polling when disposed. Failed polls are logged, metered and
    /// retried with backoff, never delivered as <c language="csharp">OnError</c>.
    /// </returns>
    public static ISubject<IEnumerable<TResult>> Observe<TResult>(
        Func<Task<IEnumerable<TResult>>> query,
        TimeSpan? pollingInterval,
        IEqualityComparer<TResult>? comparer) =>
        Observe(query, pollingInterval ?? DefaultPollingInterval, comparer, "unnamed", NullLogger.Instance, TimeProvider.System);

    /// <summary>
    /// Observes a query for a SQL storage, using the database's polling interval and live-query logger.
    /// </summary>
    /// <param name="query">Reads the current state.</param>
    /// <param name="database">The database whose polling interval and live-query logger to use.</param>
    /// <param name="name">The bounded query name used in logs and metrics.</param>
    /// <param name="comparer">The item equality comparer, or null for the default.</param>
    /// <typeparam name="TResult">The type of each item.</typeparam>
    /// <returns>A subject whose subscriptions stop polling when disposed.</returns>
    internal static ISubject<IEnumerable<TResult>> Observe<TResult>(
        Func<Task<IEnumerable<TResult>>> query,
        IDatabase database,
        string name,
        IEqualityComparer<TResult>? comparer = null) =>
        Observe(query, database.LiveQueryPollingInterval, comparer, name, database is Database sql ? sql.LiveQueryLogger : NullLogger.Instance, TimeProvider.System);

    /// <summary>
    /// Observes a query, retrying failed polls with capped exponential backoff instead of faulting the subscription.
    /// </summary>
    /// <param name="query">Reads the current state.</param>
    /// <param name="pollingInterval">The interval between successful polls, and the first retry delay.</param>
    /// <param name="comparer">The item equality comparer, or null for the default.</param>
    /// <param name="name">The bounded query name used in logs and metrics.</param>
    /// <param name="logger">The logger for poll failures and recovery.</param>
    /// <param name="timeProvider">The time provider used for delays.</param>
    /// <typeparam name="TResult">The type of each item.</typeparam>
    /// <returns>A subject whose subscriptions stop polling when disposed.</returns>
    internal static ISubject<IEnumerable<TResult>> Observe<TResult>(
        Func<Task<IEnumerable<TResult>>> query,
        TimeSpan pollingInterval,
        IEqualityComparer<TResult>? comparer,
        string name,
        ILogger logger,
        TimeProvider timeProvider)
    {
        comparer ??= EqualityComparer<TResult>.Default;

        var observable = Observable.Create<IEnumerable<TResult>>(async (observer, cancellationToken) =>
        {
            IReadOnlyCollection<TResult>? previous = null;
            var failures = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<TResult> result;
                try
                {
                    result = await query();
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception error)
                {
                    failures++;
                    var delay = RetryDelayAfter(failures, pollingInterval);
                    SqlStorageTelemetry.LiveQueryPollFailed(name, error);
                    logger.LiveQueryPollFailed(error, name, failures, delay);
                    if (!await Wait(delay, timeProvider, cancellationToken))
                    {
                        return;
                    }

                    continue;
                }

                if (failures > 0)
                {
                    logger.LiveQueryRecovered(name, failures);
                    failures = 0;
                }

                var current = result.ToArray();
                if (previous is null || !current.SequenceEqual(previous, comparer))
                {
                    observer.OnNext(current);
                    previous = current;
                }

                if (!await Wait(pollingInterval, timeProvider, cancellationToken))
                {
                    return;
                }
            }
        });

        return new ReadOnlyObservableSubject<IEnumerable<TResult>>(observable);
    }

    static TimeSpan RetryDelayAfter(int failures, TimeSpan interval)
    {
        var maximum = interval > MaximumRetryDelay ? interval : MaximumRetryDelay;
        var multiplier = 1L << Math.Min(failures - 1, 16);
        return interval.Ticks > maximum.Ticks / multiplier ? maximum : TimeSpan.FromTicks(interval.Ticks * multiplier);
    }

    static async Task<bool> Wait(TimeSpan delay, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, timeProvider, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// Adapts an <see cref="IObservable{T}"/> to an <see cref="ISubject{T}"/> whose observer side is inert.
    /// </summary>
    /// <param name="source">The observable to delegate subscriptions to.</param>
    /// <typeparam name="T">Type emitted by the observable.</typeparam>
    sealed class ReadOnlyObservableSubject<T>(IObservable<T> source) : ISubject<T>
    {
        public void OnNext(T value)
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public IDisposable Subscribe(IObserver<T> observer) => source.Subscribe(observer);
    }
}
