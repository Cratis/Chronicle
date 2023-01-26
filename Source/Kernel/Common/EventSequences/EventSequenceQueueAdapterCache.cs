// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;


public interface IEventSequenceCacheDataAdapter
{
    CachedMessage GetCachedMessage(IBatchContainer batchContainer);
}

public class EventSequenceCacheDataAdapter : IEventSequenceCacheDataAdapter, ICacheDataAdapter
{
    public IBatchContainer GetBatchContainer(ref CachedMessage cachedMessage) => throw new NotImplementedException();
    public CachedMessage GetCachedMessage(IBatchContainer batchContainer) => throw new NotImplementedException();
    public StreamSequenceToken GetSequenceToken(ref CachedMessage cachedMessage) => throw new NotImplementedException();
}


/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterCache : IQueueAdapterCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;
    readonly ICacheMonitor _cacheMonitor;
    readonly ILogger<IEvictionStrategy> _evictionStrategyLogger;
    readonly PooledQueueCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="cacheMonitor"></param>
    /// <param name="pooledQueueCacheLogger"></param>
    /// <param name="evictionStrategyLogger"></param>
    public EventSequenceQueueAdapterCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider,
        ICacheMonitor cacheMonitor,
        ILogger<PooledQueueCache> pooledQueueCacheLogger,
        ILogger<IEvictionStrategy> evictionStrategyLogger)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _cacheMonitor = cacheMonitor;
        _evictionStrategyLogger = evictionStrategyLogger;
        var dataAdapter = new EventSequenceCacheDataAdapter();
        _cache = new PooledQueueCache(dataAdapter, pooledQueueCacheLogger, cacheMonitor, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
    }

    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId) => new EventSequenceQueueCache(
        _executionContextManager,
        _eventLogStorageProvider,
        _cacheMonitor,
        _cache,
        _evictionStrategyLogger);
}
