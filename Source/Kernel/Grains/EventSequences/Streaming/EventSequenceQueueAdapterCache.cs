// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterCache : IQueueAdapterCache
{
    readonly IEventSequenceCaches _caches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
    /// </summary>
    /// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
    public EventSequenceQueueAdapterCache(IEventSequenceCaches caches)
    {
        _caches = caches;
    }

    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new EventSequenceQueueCache(_caches);
    }
}
