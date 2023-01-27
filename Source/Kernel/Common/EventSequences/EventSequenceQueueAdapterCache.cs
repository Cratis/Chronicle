// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;


/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterCache : IQueueAdapterCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;
    readonly ITelemetryProducer _telemetryProducer;
    readonly ILogger<PooledQueueCache> _pooledQueueCacheLogger;
    readonly ILogger<IEvictionStrategy> _evictionStrategyLogger;
    readonly IEventSequenceCacheDataAdapter _dataAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="dataAdapter"></param>
    /// <param name="telemetryProducer"></param>
    /// <param name="pooledQueueCacheLogger"></param>
    /// <param name="evictionStrategyLogger"></param>
    public EventSequenceQueueAdapterCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider,
        IEventSequenceCacheDataAdapter dataAdapter,
        ITelemetryProducer telemetryProducer,
        ILogger<PooledQueueCache> pooledQueueCacheLogger,
        ILogger<IEvictionStrategy> evictionStrategyLogger)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _telemetryProducer = telemetryProducer;
        _pooledQueueCacheLogger = pooledQueueCacheLogger;
        _evictionStrategyLogger = evictionStrategyLogger;
        _dataAdapter = dataAdapter;

    }

    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        var cacheMonitor = new DefaultCacheMonitor(new(queueId.ToString(), Guid.NewGuid().ToString()), _telemetryProducer);
        var cache = new PooledQueueCache(_dataAdapter, _pooledQueueCacheLogger, cacheMonitor, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
        return new EventSequenceQueueCache(
        _executionContextManager,
        _eventLogStorageProvider,
        cacheMonitor,
        cache,
        _dataAdapter,
        _evictionStrategyLogger);
    }
}
