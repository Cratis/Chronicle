// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueCache : IQueueCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;
    readonly ICacheMonitor _cacheMonitor;
    readonly PooledQueueCache _cache;
    readonly IEventSequenceCacheDataAdapter _dataAdapter;
    readonly IEvictionStrategy _evictionStrategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="cacheMonitor"></param>
    /// <param name="cache"></param>
    /// <param name="dataAdapter"></param>
    /// <param name="evictionStrategyLogger"></param>
    public EventSequenceQueueCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider,
        ICacheMonitor cacheMonitor,
        PooledQueueCache cache,
        IEventSequenceCacheDataAdapter dataAdapter,
        ILogger<IEvictionStrategy> evictionStrategyLogger)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _cacheMonitor = cacheMonitor;
        _cache = cache;
        _dataAdapter = dataAdapter;
        var timePurgePredicate = new TimePurgePredicate(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
        _evictionStrategy = new ChronologicalEvictionStrategy(evictionStrategyLogger, timePurgePredicate, cacheMonitor, TimeSpan.FromMinutes(5))
        {
            PurgeObservable = cache,
            OnPurged = OnPurge
        };
    }

    void OnPurge(CachedMessage? lastItemPurged, CachedMessage? newestItem)
    {
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

        return new Cursor(_cache, streamIdentity, token);

        /*
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
        */
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

    class Cursor : IQueueCacheCursor
    {
        private readonly PooledQueueCache _cache;
        private readonly object _cursor;
        private IBatchContainer _current;

        public Cursor(PooledQueueCache cache, IStreamIdentity streamId, StreamSequenceToken token)
        {
            _current = null!;
            _cache = cache;
            _cursor = cache.GetCursor(streamId, token);
        }

        public void Dispose()
        {
        }

        public IBatchContainer GetCurrent(out Exception exception)
        {
            exception = null!;
            return _current;
        }

        public bool MoveNext()
        {
            if (!_cache.TryGetNextMessage(_cursor, out var next))
            {
                return false;
            }

            _current = next;
            return true;
        }

        public void Refresh(StreamSequenceToken token)
        {
        }

        public void RecordDeliveryFailure()
        {
        }
    }

}
