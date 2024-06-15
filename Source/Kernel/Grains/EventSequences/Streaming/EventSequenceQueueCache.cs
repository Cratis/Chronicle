// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Events;
using Cratis.EventSequences;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
/// </remarks>
/// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
public class EventSequenceQueueCache(IEventSequenceCaches caches) : IQueueCache
{
    /// <inheritdoc/>
    public void AddToCache(IList<IBatchContainer> messages)
    {
        foreach (var message in messages.Where(_ => !_.SequenceToken.IsWarmUp()))
        {
            if (message is EventSequenceBatchContainer batchContainer)
            {
                var eventStoreAndNamespace = (EventStoreAndNamespace)message.StreamId.GetNamespace()!;
                foreach (var (@event, _) in batchContainer.GetEvents<AppendedEvent>())
                {
                    caches.GetFor(eventStoreAndNamespace.EventStore, eventStoreAndNamespace.Namespace, (EventSequenceId)message.StreamId.GetKeyAsString()).Add(@event);
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

        var eventStoreAndNamespace = (EventStoreAndNamespace)streamId.GetNamespace()!;
        var cache = caches.GetFor(
                eventStoreAndNamespace.EventStore,
                eventStoreAndNamespace.Namespace,
                (EventSequenceId)streamId.GetKeyAsString());

        if (token.SequenceNumber < (long)cache.Head.Value)
        {
            return new EmptyEventSequenceQueueCacheCursor();
        }

        return new EventSequenceQueueCacheCursor(
            cache,
            eventStoreAndNamespace.EventStore,
            eventStoreAndNamespace.Namespace,
            (EventSequenceId)streamId.GetKeyAsString(),
            (ulong)token.SequenceNumber);
    }

    /// <inheritdoc/>
    public int GetMaxAddCount() => int.MaxValue;

    /// <inheritdoc/>
    public bool IsUnderPressure() => caches.IsUnderPressure();

    /// <inheritdoc/>
    public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
    {
        purgedItems = null!;
        caches.Purge();
        return false;
    }
}
