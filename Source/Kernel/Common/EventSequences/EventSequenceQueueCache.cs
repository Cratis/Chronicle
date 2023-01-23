// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueCache : IQueueCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    public EventSequenceQueueCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
    }

    /// <inheritdoc/>
    public void AddToCache(IList<IBatchContainer> messages)
    {
    }

    /// <inheritdoc/>
    public IQueueCacheCursor GetCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token)
    {
        if (token is null)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        var microserviceAndTenant = (MicroserviceAndTenant)streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);

        if (token.IsWarmUp())
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        if (token is EventSequenceNumberTokenWithFilter tokenWithFilter)
        {
            if (tokenWithFilter.Partition == EventSourceId.Unspecified)
            {
                if (tokenWithFilter.EventTypes.Any())
                {
                    return new EventSequenceQueueCacheCursorForEventTypes(_executionContextManager, (ulong)token.SequenceNumber, streamIdentity, _eventLogStorageProvider(), tokenWithFilter.EventTypes);
                }

                return new EventSequenceQueueCacheCursor(_executionContextManager, (ulong)token.SequenceNumber, streamIdentity, _eventLogStorageProvider());
            }

            return new EventSequenceQueueCacheCursorForEventTypesAndPartition(_executionContextManager, _eventLogStorageProvider(), streamIdentity, token, tokenWithFilter.EventTypes, tokenWithFilter.Partition);
        }

        return new EventSequenceQueueCacheCursor(_executionContextManager, (ulong)token.SequenceNumber, streamIdentity, _eventLogStorageProvider());
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
