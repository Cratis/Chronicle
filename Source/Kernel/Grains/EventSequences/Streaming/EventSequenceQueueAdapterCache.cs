// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterCache : IQueueAdapterCache
{
    readonly IEventSequenceCaches _caches;
    readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
    /// </summary>
    /// <param name="caches"></param>
    /// <param name="logger"></param>
    public EventSequenceQueueAdapterCache(
        IEventSequenceCaches caches,
        ILogger logger)
    {
        _caches = caches;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new EventSequenceQueueCache(
            _caches,
            _logger,
            queueId);
    }
}
