// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.EventSequences;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCaches"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceCaches"/> class.
/// </remarks>
/// <param name="eventSequenceCacheFactory"><see cref="IEventSequenceCacheFactory"/> for creating <see cref="IEventSequenceCache"/> instances.</param>
[Singleton]
public class EventSequenceCaches(
    IEventSequenceCacheFactory eventSequenceCacheFactory) : IEventSequenceCaches
{
    readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName, EventSequenceId), IEventSequenceCache> _caches = new();

    /// <inheritdoc/>
    public IEventSequenceCache GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId)
    {
        var key = (eventStore, @namespace, eventSequenceId);
        if (!_caches.TryGetValue(key, out var cache))
        {
            cache = eventSequenceCacheFactory.Create(eventStore, @namespace, eventSequenceId);
            _caches.TryAdd(key, cache);
        }

        return cache;
    }

    /// <inheritdoc/>
    public bool IsUnderPressure() => _caches.Values.Any(_ => _.IsUnderPressure());

    /// <inheritdoc/>
    public async Task PrimeAll()
    {
        // TODO: This needs to be implemented properly. Or if we get to rewrite how we append to event sequences first, we can remove this entire file.
        // foreach (var (microserviceId, microservice) in configuration.Microservices)
        // {
        //     foreach (var (tenantId, _) in configuration.Tenants)
        //     {
        //         if (!configuration.Storage.Microservices.ContainsKey(microserviceId) ||
        //             !configuration.Storage.Microservices.Get(microserviceId).Tenants.ContainsKey(tenantId))
        //         {
        //             continue;
        //         }
        //         await GetFor(microserviceId, tenantId, EventSequenceId.Log).PrimeWithTailWindow();
        //     }
        // }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Purge()
    {
        foreach (var cache in _caches.Values.Where(_ => _.IsUnderPressure()))
        {
            cache.Purge();
        }
    }
}
