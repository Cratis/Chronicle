// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
/// </remarks>
/// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
public class EventSequenceQueueAdapterCache(IEventSequenceCaches caches) : IQueueAdapterCache
{
    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new EventSequenceQueueCache(caches);
    }
}
