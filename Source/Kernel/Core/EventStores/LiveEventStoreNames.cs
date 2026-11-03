// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Builds the live list of event store names, retrying stores that fail to initialize until they appear.
/// </summary>
internal static class LiveEventStoreNames
{
    static readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(1);
    static readonly TimeSpan _maximumRetryDelay = TimeSpan.FromSeconds(30);

    static readonly IEqualityComparer<EventStoreName[]> _sameStores = EqualityComparer<EventStoreName[]>.Create(
        (previous, current) => previous is not null && current is not null && previous.ToHashSet().SetEquals(current),
        stores => stores.Length);

    /// <summary>
    /// Observes the names of all initialized event stores.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe event stores from.</param>
    /// <param name="initialization">The event store initialization service.</param>
    /// <param name="scheduler">The <see cref="IScheduler"/> retries are scheduled on.</param>
    /// <returns>An observable subject emitting collections of event store names.</returns>
    public static ISubject<IEnumerable<EventStoreNames>> Observe(IStorage storage, IEventStoreInitialization initialization, IScheduler scheduler)
    {
        var observable = storage.ObserveEventStores()
            .Select(stores => ObserveInitializedStores([.. stores], initialization, scheduler).DistinctUntilChanged(_sameStores))
            .Switch()
            .Select(stores => (IEnumerable<EventStoreNames>)[.. stores.Select(eventStore => new EventStoreNames(eventStore.Value, eventStore.Value))]);
        return Subject.Create<IEnumerable<EventStoreNames>>(Observer.Create<IEnumerable<EventStoreNames>>(_ => { }), observable);
    }

    /// <summary>
    /// Initializes the stored set and keeps retrying the stores that failed, with a doubling delay, until all are initialized.
    /// </summary>
    /// <param name="stores">The stored set of event stores.</param>
    /// <param name="initialization">The event store initialization service.</param>
    /// <param name="scheduler">The <see cref="IScheduler"/> retries are scheduled on.</param>
    /// <returns>An observable emitting the initialized stores after every attempt.</returns>
    /// <remarks>
    /// Each retry replaces the previous attempt and timer instead of nesting subscriptions, so a store that keeps
    /// failing costs constant memory for as long as the list is observed.
    /// </remarks>
    static IObservable<EventStoreName[]> ObserveInitializedStores(EventStoreName[] stores, IEventStoreInitialization initialization, IScheduler scheduler) =>
        Observable.Create<EventStoreName[]>(observer =>
        {
            var attempt = new SerialDisposable();
            var timer = new SerialDisposable();

            void Attempt(TimeSpan delay) =>
                attempt.Disposable = Observable.FromAsync(async () => (await initialization.EnsureAll(stores)).ToArray())
                    .Subscribe(
                        initialized =>
                        {
                            observer.OnNext(initialized);
                            if (initialized.Length >= stores.Length)
                            {
                                observer.OnCompleted();
                                return;
                            }

                            var next = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _maximumRetryDelay.Ticks));
                            timer.Disposable = scheduler.Schedule(delay, () => Attempt(next));
                        },
                        observer.OnError);

            Attempt(_initialRetryDelay);
            return new CompositeDisposable(attempt, timer);
        });
}
