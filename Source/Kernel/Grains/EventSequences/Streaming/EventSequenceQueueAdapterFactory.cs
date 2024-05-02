// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapterFactory"/> for our persistent event store.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceQueueAdapter"/> class.
/// </remarks>
/// <param name="name">Name of stream.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="caches">All the <see cref="IEventSequenceCaches"/>.</param>
public class EventSequenceQueueAdapterFactory(
    string name,
    IStorage storage,
    IEventSequenceCaches caches) : IQueueAdapterFactory
{
    readonly IQueueAdapterCache _cache = new EventSequenceQueueAdapterCache(caches);
    readonly IStreamQueueMapper _mapper = new HashRingBasedStreamQueueMapper(new(), name);

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
            serviceProvider.GetRequiredService<IStorage>(),
            serviceProvider.GetRequiredService<IEventSequenceCaches>());
    }

    /// <inheritdoc/>
    public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventSequenceQueueAdapter(name, _mapper, storage));

    /// <inheritdoc/>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

    /// <inheritdoc/>
    public IQueueAdapterCache GetQueueAdapterCache() => _cache;

    /// <inheritdoc/>
    public IStreamQueueMapper GetStreamQueueMapper() => _mapper;
}
