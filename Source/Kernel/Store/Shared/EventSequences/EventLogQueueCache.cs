// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
public class EventLogQueueCache : IQueueCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventLogStorageProvider> _eventLogStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogQueueCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
    public EventLogQueueCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventLogStorageProvider> eventLogStorageProvider)
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
        var microserviceAndTenant = (MicroserviceAndTenant)streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);

        if (token is EventLogSequenceNumberTokenWithFilter tokenWithFilter)
        {
            return new EventLogQueueCacheCursor(_executionContextManager, _eventLogStorageProvider(), streamIdentity, token, tokenWithFilter.EventTypes, tokenWithFilter.Partition);
        }

        return new EventLogQueueCacheCursor(_executionContextManager, _eventLogStorageProvider(), streamIdentity, token);
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
