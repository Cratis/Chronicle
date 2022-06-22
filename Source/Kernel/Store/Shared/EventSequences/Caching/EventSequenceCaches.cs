// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCaches"/>.
/// </summary>
[Singleton]
public class EventSequenceCaches : IEventSequenceCaches
{
    readonly ConcurrentDictionary<EventSequenceCacheKey, IEventSequenceCache> _caches = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCaches"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    public EventSequenceCaches(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
    }

    /// <inheritdoc/>
    public IEventSequenceCache GetFor(EventSequenceCacheKey key)
    {
        if (_caches.ContainsKey(key))
        {
            return _caches[key];
        }

        _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
        return _caches[key] = new EventSequenceCache(key.EventSequenceId, 500, _eventLogStorageProvider());
    }
}
