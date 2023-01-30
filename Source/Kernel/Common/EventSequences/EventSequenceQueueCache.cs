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
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    readonly IEventSequenceCaches _caches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventSequenceStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="caches"></param>
    public EventSequenceQueueCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        IEventSequenceCaches caches)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _caches = caches;
    }

    /// <inheritdoc/>
    public void AddToCache(IList<IBatchContainer> messages)
    {
        foreach (var message in messages.Where(_ => !_.SequenceToken.IsWarmUp()))
        {
            if (message is EventSequenceBatchContainer batchContainer)
            {
                var microserviceAndTenant = (MicroserviceAndTenant)message.StreamNamespace;
                foreach (var (@event, _) in batchContainer.GetEvents<AppendedEvent>())
                {
                    _caches.GetFor(microserviceAndTenant.MicroserviceId, microserviceAndTenant.TenantId, (EventSequenceId)message.StreamGuid).Add(@event);
                }
            }
        }
    }

    /// <inheritdoc/>
    public IQueueCacheCursor GetCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token)
    {
        if (token is null)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        if (token.IsWarmUp())
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        var microserviceAndTenant = (MicroserviceAndTenant)streamIdentity.Namespace;
        return new EventSequenceQueueCacheCursor(
            _caches.GetFor(
                microserviceAndTenant.MicroserviceId,
                microserviceAndTenant.TenantId,
                (EventSequenceId)streamIdentity.Guid),
            _executionContextManager,
            _eventSequenceStorageProvider,
            microserviceAndTenant.MicroserviceId,
            microserviceAndTenant.TenantId,
            (EventSequenceId)streamIdentity.Guid,
            (ulong)token.SequenceNumber);
    }

    /// <inheritdoc/>
    public int GetMaxAddCount() => int.MaxValue;

    /// <inheritdoc/>
    public bool IsUnderPressure() => false;

    /// <inheritdoc/>
    public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
    {
        purgedItems = null!;
        return false;
    }
}
