// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Store.EventSequences.Caching;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueCache : IQueueCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;
    readonly IEventSequenceCaches _caches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="caches">Global <see cref="IEventSequenceCaches"/>.</param>
    public EventSequenceQueueCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider,
        IEventSequenceCaches caches)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _caches = caches;
    }

    /// <inheritdoc/>
    public void AddToCache(IList<IBatchContainer> messages)
    {
        foreach (var message in messages)
        {
            if (message is EventSequenceBatchContainer container && container.SequenceToken.SequenceNumber != -1)
            {
                var events = container.GetEvents<AppendedEvent>();
                _caches.GetFor(new(container.StreamGuid, container.MicroserviceId, container.TenantId)).Feed(events.Select(_ => _.Item1).ToArray());
            }
        }
    }

    /// <inheritdoc/>
    public IQueueCacheCursor GetCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token)
    {
        var microserviceAndTenant = (MicroserviceAndTenant)streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);

        if (token.SequenceNumber == -1)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        var cache = _caches.GetFor(new(streamIdentity.Guid, microserviceAndTenant.MicroserviceId, microserviceAndTenant.TenantId));
        if (token is EventSequenceNumberTokenWithFilter tokenWithFilter)
        {
            if (tokenWithFilter.Partition == EventSourceId.Unspecified)
            {
                if (tokenWithFilter.EventTypes.Any())
                {
                    return new EventSequenceQueueCacheCursorForEventTypes(cache.GetFrom((ulong)token.SequenceNumber), streamIdentity, tokenWithFilter.EventTypes);
                }

                return new EventSequenceQueueCacheCursor(cache.GetFrom((ulong)token.SequenceNumber), streamIdentity);
            }

            return new EventSequenceQueueCacheCursorForEventTypesAndPartition(_executionContextManager, _eventLogStorageProvider(), streamIdentity, token, tokenWithFilter.EventTypes, tokenWithFilter.Partition);
        }

        return new EventSequenceQueueCacheCursor(cache.GetFrom((ulong)token.SequenceNumber), streamIdentity);
    }

    /// <inheritdoc/>
    public int GetMaxAddCount() => int.MaxValue;

    /// <inheritdoc/>
    public bool IsUnderPressure() => false;

    /// <inheritdoc/>
    public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
    {
        purgedItems = new List<IBatchContainer>();
        return true;
    }
}
