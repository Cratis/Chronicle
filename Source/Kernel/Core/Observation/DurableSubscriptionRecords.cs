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
/// <param name="localSilo">The <see cref="ILocalSiloDetails"/> of this silo; a record names a client of this silo only when its target says so.</param>
[Singleton]
public class DurableSubscriptionRecords(
    IStorage storage,
    IOptions<ChronicleOptions> options,
    IClusterReleases? clusterReleases = null,
    TimeProvider? timeProvider = null,
    ILocalSiloDetails? localSilo = null) : IDurableSubscriptionRecords
{
    /// <summary>
    /// How long the records of one event sequence in a namespace are reused.
    /// </summary>
    internal static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How old the shared read must be before an observer missing from it is looked for in storage.
    /// </summary>
    internal static readonly TimeSpan RecheckMissAfter = TimeSpan.FromSeconds(2);

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
        if (records.TryGetValue(observer.ObserverId, out var record))
        {
            return record;
        }

        // An observer that subscribed after the shared read was taken is missing from it, and a missing record sends the caller
        // to the observer itself, which would wake it. So a miss is checked against storage - but only once the shared read
        // is old enough that several asks in a row cannot turn into a read each.
        var key = (observer.EventStore, observer.Namespace, observer.EventSequenceId);
        if (_reads.TryGetValue(key, out var cached) && _clock.GetUtcNow() - cached.ReadAt >= RecheckMissAfter)
        {
            records = await GetAll(observer.EventStore, observer.Namespace, observer.EventSequenceId, fresh: true);
            return records.TryGetValue(observer.ObserverId, out record) ? record : null;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> Names(ObserverKey observer, ConnectionId connectionId) =>
        await Get(observer) is { } record &&
        (record.Targets.Any(target => target.ConnectionId == connectionId.Value && IsThisSilo(target.Silo)) ||
         (record.Standbys?.Any(standby => standby.ConnectionId == connectionId.Value && IsThisSilo(standby.Silo)) ?? false));

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

    bool IsThisSilo(string silo) => localSilo is null || silo == localSilo.SiloAddress.ToParsableString();

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
