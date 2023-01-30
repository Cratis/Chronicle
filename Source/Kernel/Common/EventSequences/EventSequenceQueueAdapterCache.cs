// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;


/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapterCache : IQueueAdapterCache
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventLogStorageProvider;
    readonly IEventSequenceCaches _caches;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapterCache"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="caches"></param>
    public EventSequenceQueueAdapterCache(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider,
        IEventSequenceCaches caches)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _caches = caches;
    }

    /// <inheritdoc/>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new EventSequenceQueueCache(
            _executionContextManager,
            _eventLogStorageProvider,
            _caches);
    }
}
