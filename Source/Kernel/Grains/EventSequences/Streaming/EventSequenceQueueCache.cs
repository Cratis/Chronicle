// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.EventSequences;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueCache : IQueueCache
{
    readonly IEventSequenceCaches _caches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
    /// </summary>
    /// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
    public EventSequenceQueueCache(IEventSequenceCaches caches)
    {
        _caches = caches;
    }

    /// <inheritdoc/>
    public void AddToCache(IList<IBatchContainer> messages)
    {
        foreach (var message in messages.Where(_ => !_.SequenceToken.IsWarmUp()))
        {
            if (message is EventSequenceBatchContainer batchContainer)
            {
                var microserviceAndTenant = (MicroserviceAndTenant)message.StreamId.GetNamespace()!;
                foreach (var (@event, _) in batchContainer.GetEvents<AppendedEvent>())
                {
                    _caches.GetFor(microserviceAndTenant.MicroserviceId, microserviceAndTenant.TenantId, (EventSequenceId)message.StreamId.GetKeyAsString()).Add(@event);
                }
            }
        }
    }

    /// <inheritdoc/>
    public IQueueCacheCursor GetCacheCursor(StreamId streamId, StreamSequenceToken token)
    {
        if (token is null)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        if (token.IsWarmUp())
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        var microserviceAndTenant = (MicroserviceAndTenant)streamId.GetNamespace()!;
        var cache = _caches.GetFor(
                microserviceAndTenant.MicroserviceId,
                microserviceAndTenant.TenantId,
                (EventSequenceId)streamId.GetKeyAsString());

        if (token.SequenceNumber < (long)cache.Head.Value)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        return new EventSequenceQueueCacheCursor(
            cache,
            microserviceAndTenant.MicroserviceId,
            microserviceAndTenant.TenantId,
            (EventSequenceId)streamId.GetKeyAsString(),
            (ulong)token.SequenceNumber);
    }

    /// <inheritdoc/>
    public int GetMaxAddCount() => int.MaxValue;

    /// <inheritdoc/>
    public bool IsUnderPressure() => _caches.IsUnderPressure();

    /// <inheritdoc/>
    public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
    {
        purgedItems = null!;
        _caches.Purge();
        return false;
    }
}
