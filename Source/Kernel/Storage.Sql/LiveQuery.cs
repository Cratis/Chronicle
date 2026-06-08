// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Helpers for building live, database-watching observable queries for the SQL storage backends.
/// </summary>
/// <remarks>
/// MongoDB delivers live updates through collection change-streams. The SQL backends cannot use Arc's
/// push-based <c>DbSet.Observe()</c> here because every event store and namespace lives in its own
/// database created on demand with a per-store connection string — those contexts are not registered
/// in dependency injection, which <c>DbSet.Observe()</c> requires to build its listener. Instead this
/// polls the supplied query on an interval and re-emits only when the result actually changes, giving
/// subscribers automatic updates without a manual refresh while honoring the per-store connection.
/// </remarks>
public static class LiveQuery
{
    /// <summary>
    /// The default interval between polls of the underlying database.
    /// </summary>
    public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(2);

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
    /// into it. Polling stops when the subscriber unsubscribes.
    /// </returns>
    public static ISubject<IEnumerable<TResult>> Observe<TResult>(
        Func<Task<IEnumerable<TResult>>> query,
        TimeSpan? pollingInterval = null)
    {
        var interval = pollingInterval ?? DefaultPollingInterval;

        var observable = Observable.Create<IEnumerable<TResult>>(async (observer, cancellationToken) =>
        {
            IReadOnlyCollection<TResult>? previous = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var current = (await query()).ToArray();
                if (previous is null || !current.SequenceEqual(previous))
                {
                    observer.OnNext(current);
                    previous = current;
                }

                await Task.Delay(interval, cancellationToken);
            }
        });

        return new ReadOnlyObservableSubject<IEnumerable<TResult>>(observable);
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
