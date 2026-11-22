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
/// appearing in storage, the observers of this silo that were admitted are remembered, in the order they asked. An
/// observer is only held back by those that asked before it, so two observers never hold each other back.
/// </para>
/// <para>
/// A listing that fails admits the observer: the limit protects the kernel from a burst and must not stop catch-up
/// when the job storage cannot answer.
/// </para>
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> holding the jobs.</param>
[Singleton]
public class CatchUpAdmission(IStorage storage) : ICatchUpAdmission
{
    readonly Lock _lock = new();
    readonly Dictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace), List<ObserverKey>> _admitted = [];

    /// <inheritdoc/>
    public async Task<IDisposable?> TryAdmit(ObserverKey observerKey, int limit)
    {
        if (limit <= 0)
        {
            return NoLease.Instance;
        }

        var scope = (observerKey.EventStore, observerKey.Namespace);
        var ahead = Reserve(scope, observerKey);
        var lease = new Lease(this, scope, observerKey);
        try
        {
            var running = await CountRunningCatchUps(observerKey);
            if (running.Own || running.Others + ahead < limit)
            {
                return lease;
            }
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        lease.Dispose();
        return null;
    }

    int Reserve((EventStoreName EventStore, EventStoreNamespaceName Namespace) scope, ObserverKey observerKey)
    {
        lock (_lock)
        {
            if (!_admitted.TryGetValue(scope, out var keys))
            {
                _admitted[scope] = keys = [];
            }

            var ahead = keys.Count(_ => _ != observerKey);
            keys.Add(observerKey);
            return ahead;
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
