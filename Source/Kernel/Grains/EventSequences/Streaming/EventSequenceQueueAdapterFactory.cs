// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.EventSequences;
using Aksio.Cratis.Kernel.Persistence.Identities;
using Aksio.DependencyInversion;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterFactory"/> for our persistent event store.
/// </summary>
public class EventSequenceQueueAdapterFactory : IQueueAdapterFactory
{
    readonly IQueueAdapterCache _cache;
    readonly IStreamQueueMapper _mapper;
    readonly string _name;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ProviderFor<IIdentityStorage> _identityStoreProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapter"/> class.
    /// </summary>
    /// <param name="name">Name of stream.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="identityStoreProvider">Provider for <see cref="IIdentityStorage"/>.</param>
    /// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
    public EventSequenceQueueAdapterFactory(
        string name,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ProviderFor<IIdentityStorage> identityStoreProvider,
        IEventSequenceCaches caches)
    {
        _mapper = new HashRingBasedStreamQueueMapper(new(), name);
        _cache = new EventSequenceQueueAdapterCache(caches);
        _name = name;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _identityStoreProvider = identityStoreProvider;
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
            serviceProvider.GetRequiredService<ProviderFor<IEventSequenceStorage>>(),
            serviceProvider.GetRequiredService<ProviderFor<IIdentityStorage>>(),
            serviceProvider.GetRequiredService<IEventSequenceCaches>());
    }

    /// <inheritdoc/>
    public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventSequenceQueueAdapter(_name, _mapper, _eventSequenceStorageProvider, _identityStoreProvider));

    /// <inheritdoc/>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

    /// <inheritdoc/>
    public IQueueAdapterCache GetQueueAdapterCache() => _cache;

    /// <inheritdoc/>
    public IStreamQueueMapper GetStreamQueueMapper() => _mapper;
}
