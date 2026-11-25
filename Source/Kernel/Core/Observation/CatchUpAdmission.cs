// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICatchUpAdmission"/> counting the catch-up jobs running in the namespace.
/// </summary>
/// <remarks>
/// <para>
/// The running jobs are read from the job storage, so the limit holds across the cluster. Between that read and the job
/// appearing in storage, the observers of this silo that were admitted are remembered. An observer is only held back by
/// those that were admitted before it, and one that is still waiting for the job listing holds nobody back, so observers
/// that ask at the same time never hold each other back and leave the places unused.
/// </para>
/// <para>
/// An observer that is held back is remembered with how far behind it is. When a place is free it goes to the observers
/// furthest behind, the one that has waited longest first among equals, so a burst drains the largest backlogs first
/// instead of in whatever order the retries happen to land. A held-back observer that stops asking (it was removed, or its
/// client went away) is forgotten after <see cref="WaiterLifetime"/>, so it never holds a place back for good.
/// </para>
/// <para>
/// A listing that fails admits the observer: the limit protects the kernel from a burst and must not stop catch-up
/// when the job storage cannot answer.
/// </para>
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> holding the jobs.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> telling how long an observer has been held back.</param>
[Singleton]
public class CatchUpAdmission(IStorage storage, TimeProvider? timeProvider = null) : ICatchUpAdmission
{
    /// <summary>
    /// How long a held-back observer keeps its turn without asking again.
    /// </summary>
    /// <remarks>
    /// It is a few times the longest delay before a held-back observer asks again.
    /// </remarks>
    internal static readonly TimeSpan WaiterLifetime = TimeSpan.FromSeconds(30);

    readonly Lock _lock = new();
    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Dictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace), List<ObserverKey>> _admitted = [];
    readonly Dictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace), Dictionary<ObserverKey, Waiter>> _waiting = [];

    /// <inheritdoc/>
    public async Task<IDisposable?> TryAdmit(ObserverKey observerKey, int limit, ulong eventsBehind)
    {
        if (limit <= 0)
        {
            return NoLease.Instance;
        }

        var scope = (observerKey.EventStore, observerKey.Namespace);
        var running = await CountRunningCatchUps(observerKey);
        if (running.Own)
        {
            Forget(scope, observerKey);
            return new Lease(this, scope, observerKey);
        }

        return TryTakePlace(scope, observerKey, eventsBehind, limit - running.Others) ? new Lease(this, scope, observerKey) : null;
    }

    void Forget((EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey)
    {
        lock (_lock)
        {
            if (_waiting.TryGetValue(scope, out var waiters) && waiters.Remove(observerKey) && waiters.Count == 0)
            {
                _waiting.Remove(scope);
            }
        }
    }

    bool TryTakePlace((EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey, ulong eventsBehind, int placesNotRunning)
    {
        var now = _clock.GetUtcNow();

        // The decision and the reservation are one step: nobody can be admitted in between, and an observer that is
        // refused never counted against the ones deciding after it.
        lock (_lock)
        {
            var admittedAhead = _admitted.TryGetValue(scope, out var admitted) ? admitted.Count(_ => _ != observerKey) : 0;
            var freePlaces = placesNotRunning - admittedAhead;
            if (!_waiting.TryGetValue(scope, out var waiters))
            {
                _waiting[scope] = waiters = [];
            }

            foreach (var expired in waiters.Where(_ => now - _.Value.LastAsked > WaiterLifetime).Select(_ => _.Key).ToArray())
            {
                waiters.Remove(expired);
            }

            var since = waiters.TryGetValue(observerKey, out var existing) ? existing.Since : now;
            var hasPlace = freePlaces > 0 && waiters.Count(_ => _.Key != observerKey && _.Value.IsAheadOf(eventsBehind, since)) < freePlaces;
            if (hasPlace)
            {
                waiters.Remove(observerKey);
                Reserve(scope, observerKey);
            }
            else
            {
                waiters[observerKey] = new(eventsBehind, since, now);
            }

            if (waiters.Count == 0)
            {
                _waiting.Remove(scope);
            }

            return hasPlace;
        }
    }

    void Reserve((EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey)
    {
        lock (_lock)
        {
            if (!_admitted.TryGetValue(scope, out var keys))
            {
                _admitted[scope] = keys = [];
            }

            keys.Add(observerKey);
        }
    }

    void Release((EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey)
    {
        lock (_lock)
        {
            if (!_admitted.TryGetValue(scope, out var keys))
            {
                return;
            }

            keys.Remove(observerKey);
            if (keys.Count == 0)
            {
                _admitted.Remove(scope);
            }
        }
    }

    async Task<(bool Own, int Others)> CountRunningCatchUps(ObserverKey observerKey)
    {
        var listing = await storage
            .GetEventStore(observerKey.EventStore)
            .GetNamespace(observerKey.Namespace)
            .Jobs
            .GetOutstandingJobs();
        if (!listing.TryGetResult(out var jobs))
        {
            return (true, 0);
        }

        var now = DateTimeOffset.UtcNow;
        var catchUps = jobs
            .Where(_ => _.Request is CatchUpObserverRequest)
            .ToArray();
        var own = catchUps.Any(_ => ((CatchUpObserverRequest)_.Request).ObserverKey == observerKey);
        var others = catchUps
            .Where(_ => _.IsLive(now))
            .Select(_ => ((CatchUpObserverRequest)_.Request).ObserverKey)
            .Where(_ => _ != observerKey)
            .Distinct()
            .Count();
        return (own, others);
    }

    sealed record Waiter(ulong EventsBehind, DateTimeOffset Since, DateTimeOffset LastAsked)
    {
        internal bool IsAheadOf(ulong eventsBehind, DateTimeOffset since) =>
            EventsBehind > eventsBehind || (EventsBehind == eventsBehind && Since < since);
    }

    sealed class NoLease : IDisposable
    {
        internal static readonly NoLease Instance = new();

        public void Dispose()
        {
        }
    }

    sealed class Lease(CatchUpAdmission admission, (EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey) : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                admission.Release(scope, observerKey);
            }
        }
    }
}
