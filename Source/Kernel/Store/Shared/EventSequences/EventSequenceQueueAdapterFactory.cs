// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterFactory"/> for our persistent event store.
/// </summary>
public class EventSequenceQueueAdapterFactory : IQueueAdapterFactory
{
    readonly IQueueAdapterCache _cache;
    readonly IStreamQueueMapper _mapper;
    readonly string _name;
    readonly ProviderFor<IEventSequences> _eventLogsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapter"/> class.
    /// </summary>
    /// <param name="name">Name of stream.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventLogsProvider">Provider for <see cref="IEventSequences"/>.</param>
    /// <param name="eventLogStorageProvider">Provider for <see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    public EventSequenceQueueAdapterFactory(
        string name,
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequences> eventLogsProvider,
        ProviderFor<IEventSequenceStorageProvider> eventLogStorageProvider)
    {
        _mapper = new HashRingBasedStreamQueueMapper(new(), name);
        _cache = new EventSequenceQueueAdapterCache(executionContextManager, eventLogStorageProvider);
        _name = name;
        _eventLogsProvider = eventLogsProvider;
    }

    /// <summary>
    /// Creates a <see cref="EventSequenceQueueAdapterFactory"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to use for service dependencies.</param>
    /// <param name="name">Name of stream.</param>
    /// <returns>A new <see cref="EventSequenceQueueAdapter"/>.</returns>
    public static EventSequenceQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        return new(
            name,
            serviceProvider.GetService<IExecutionContextManager>()!,
            serviceProvider.GetService<ProviderFor<IEventSequences>>()!,
            serviceProvider.GetService<ProviderFor<IEventSequenceStorageProvider>>()!);
    }

    /// <inheritdoc/>
    public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventSequenceQueueAdapter(_name, _mapper, _eventLogsProvider));

    /// <inheritdoc/>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

    /// <inheritdoc/>
    public IQueueAdapterCache GetQueueAdapterCache() => _cache;

    /// <inheritdoc/>
    public IStreamQueueMapper GetStreamQueueMapper() => _mapper;
}
