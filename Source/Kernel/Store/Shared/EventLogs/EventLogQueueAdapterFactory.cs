// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventLogs;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterFactory"/> for our persistent event store.
/// </summary>
public class EventLogQueueAdapterFactory : IQueueAdapterFactory
{
    readonly IQueueAdapterCache _cache;
    readonly IStreamQueueMapper _mapper;
    readonly string _name;
    readonly ProviderFor<IEventSequences> _eventLogsProvder;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogQueueAdapter"/> class.
    /// </summary>
    /// <param name="name">Name of stream.</param>
    /// <param name="eventLogsProvder">Provider for <see cref="IEventSequences"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
    public EventLogQueueAdapterFactory(
        string name,
        ProviderFor<IEventSequences> eventLogsProvder,
        IExecutionContextManager executionContextManager,
        IEventLogStorageProvider eventLogStorageProvider)
    {
        _mapper = new HashRingBasedStreamQueueMapper(new(), name);
        _cache = new EventLogQueueAdapterCache(executionContextManager, eventLogStorageProvider);
        _name = name;
        _eventLogsProvder = eventLogsProvder;
    }

    /// <summary>
    /// Creates a <see cref="EventLogQueueAdapterFactory"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to use for service dependencies.</param>
    /// <param name="name">Name of stream.</param>
    /// <returns>A new <see cref="EventLogQueueAdapter"/>.</returns>
    public static EventLogQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        return new(
            name,
            serviceProvider.GetService<ProviderFor<IEventSequences>>()!,
            serviceProvider.GetService<IExecutionContextManager>()!,
            serviceProvider.GetService<IEventLogStorageProvider>()!);
    }

    /// <inheritdoc/>
    public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventLogQueueAdapter(_name, _mapper, _eventLogsProvder));

    /// <inheritdoc/>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

    /// <inheritdoc/>
    public IQueueAdapterCache GetQueueAdapterCache() => _cache;

    /// <inheritdoc/>
    public IStreamQueueMapper GetStreamQueueMapper() => _mapper;
}
