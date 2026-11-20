// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IDurableSubscriptionRecords"/>.
/// </summary>
/// <remarks>
/// The records of an event sequence in a namespace are read as one set and reused for <see cref="CacheFor"/>, which is
/// short enough that a repaired or cleared record is seen within the keeper's own cadence, and long enough that the streams
/// of a client, each checking at its own jittered interval, share one read. A failed read is not kept.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to read from.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="clusterReleases">The <see cref="IClusterReleases"/> telling whether every silo supports dormant observers.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> for ageing the cache.</param>
[Singleton]
public class DurableSubscriptionRecords(
    IStorage storage,
    IOptions<ChronicleOptions> options,
    IClusterReleases? clusterReleases = null,
    TimeProvider? timeProvider = null) : IDurableSubscriptionRecords
{
    /// <summary>
    /// How long the records of one event sequence in a namespace are reused.
    /// </summary>
    internal static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId Sequence), CachedRead> _reads = new();

    /// <inheritdoc/>
    public bool CanBeRelied =>
        options.Value.Observers.DurableSubscriptions == DurableSubscriptionMode.Release &&
        options.Value.Observers.DormantAfter > 0 &&
        DormantObserverFloor.IsReached(clusterReleases);

    /// <inheritdoc/>
    public bool ConfirmsClientSubscriptions => CanBeRelied && options.Value.ConnectedClients.KeeperMode == ClientKeeperMode.SiloLocal;

    /// <inheritdoc/>
    public async Task<ObserverDurableSubscription?> Get(ObserverKey observer)
    {
        var records = await GetAll(observer.EventStore, observer.Namespace, observer.EventSequenceId);
        return records.TryGetValue(observer.ObserverId, out var record) ? record : null;
    }

    /// <inheritdoc/>
    public async Task<bool> Names(ObserverKey observer, ConnectionId connectionId) =>
        await Get(observer) is { } record && record.Targets.Any(target => target.ConnectionId == connectionId.Value);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<ObserverId, ObserverDurableSubscription>> GetAll(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, bool fresh = false)
    {
        var now = _clock.GetUtcNow();
        Forget(now);

        var key = (eventStore, @namespace, eventSequenceId);
        if (!fresh && _reads.TryGetValue(key, out var cached) && now - cached.ReadAt < CacheFor)
        {
            return cached.Records;
        }

        var read = new CachedRead(now, Read(key));
        _reads[key] = read;
        return read.Records;
    }

    async Task<IReadOnlyDictionary<ObserverId, ObserverDurableSubscription>> Read((EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId Sequence) key)
    {
        try
        {
            return await storage
                .GetEventStore(key.EventStore)
                .GetNamespace(key.Namespace)
                .Observers
                .GetDurableSubscriptions(key.Sequence);
        }
        catch
        {
            _reads.TryRemove(key, out _);
            throw;
        }
    }

    void Forget(DateTimeOffset now)
    {
        foreach (var (key, read) in _reads)
        {
            if (now - read.ReadAt >= CacheFor)
            {
                _reads.TryRemove(new KeyValuePair<(EventStoreName, EventStoreNamespaceName, EventSequenceId), CachedRead>(key, read));
            }
        }
    }

    sealed record CachedRead(DateTimeOffset ReadAt, Task<IReadOnlyDictionary<ObserverId, ObserverDurableSubscription>> Records);
}
