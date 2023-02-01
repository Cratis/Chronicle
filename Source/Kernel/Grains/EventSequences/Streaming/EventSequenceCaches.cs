// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCaches"/>.
/// </summary>
[Singleton]
public class EventSequenceCaches : IEventSequenceCaches
{
    readonly ConcurrentDictionary<(MicroserviceId, TenantId, EventSequenceId), IEventSequenceCache> _caches = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    readonly ILogger<EventSequenceCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCaches"/> class.
    /// </summary>
    /// <param name="executionContextManager"></param>
    /// <param name="eventSequenceStorageProvider"></param>
    /// <param name="logger"></param>
    public EventSequenceCaches(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        ILogger<EventSequenceCache> logger)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEventSequenceCache GetFor(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId)
    {
        var key = (microserviceId, tenantId, eventSequenceId);
        if (!_caches.TryGetValue(key, out var cache))
        {
            cache = new EventSequenceCache(microserviceId, tenantId, eventSequenceId, _executionContextManager, _eventSequenceStorageProvider, _logger);
            _caches.TryAdd(key, cache);
        }

        return cache;
    }

    /// <inheritdoc/>
    public bool IsUnderPressure() => _caches.Values.Any(_ => _.IsUnderPressure());

    /// <inheritdoc/>
    public void Purge()
    {
        foreach (var cache in _caches.Values.Where(_ => _.IsUnderPressure()))
        {
            cache.Purge();
        }
    }
}
