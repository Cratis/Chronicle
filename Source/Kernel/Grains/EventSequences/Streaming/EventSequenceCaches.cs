// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.EventSequences;
using Cratis.Kernel.Configuration;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCaches"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceCaches"/> class.
/// </remarks>
/// <param name="eventSequenceCacheFactory"><see cref="IEventSequenceCacheFactory"/> for creating <see cref="IEventSequenceCache"/> instances.</param>
/// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
[Singleton]
public class EventSequenceCaches(
    IEventSequenceCacheFactory eventSequenceCacheFactory,
    KernelConfiguration configuration) : IEventSequenceCaches
{
    readonly ConcurrentDictionary<(MicroserviceId, TenantId, EventSequenceId), IEventSequenceCache> _caches = new();

    /// <inheritdoc/>
    public IEventSequenceCache GetFor(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId)
    {
        var key = (microserviceId, tenantId, eventSequenceId);
        if (!_caches.TryGetValue(key, out var cache))
        {
            cache = eventSequenceCacheFactory.Create(microserviceId, tenantId, eventSequenceId);
            _caches.TryAdd(key, cache);
        }

        return cache;
    }

    /// <inheritdoc/>
    public bool IsUnderPressure() => _caches.Values.Any(_ => _.IsUnderPressure());

    /// <inheritdoc/>
    public async Task PrimeAll()
    {
        foreach (var (microserviceId, microservice) in configuration.Microservices)
        {
            foreach (var (tenantId, _) in configuration.Tenants)
            {
                if (!configuration.Storage.Microservices.ContainsKey(microserviceId) ||
                    !configuration.Storage.Microservices.Get(microserviceId).Tenants.ContainsKey(tenantId))
                {
                    continue;
                }

                await GetFor(microserviceId, tenantId, EventSequenceId.Log).PrimeWithTailWindow();
            }
        }
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
