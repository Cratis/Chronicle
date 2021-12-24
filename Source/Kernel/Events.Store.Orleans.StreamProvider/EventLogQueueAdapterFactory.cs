// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueAdapterFactory : IQueueAdapterFactory
    {
        readonly IStreamQueueMapper _mapper;
        readonly IQueueAdapterCache _adapterCache;
        readonly string _name;

        public EventLogQueueAdapterFactory(string name, ILoggerFactory loggerFactory)
        {
            _mapper = new HashRingBasedStreamQueueMapper(new(), name);
            _adapterCache = new SimpleQueueAdapterCache(new(), name, loggerFactory);
            _name = name;
        }

        /// <inheritdoc/>
        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventLogQueueAdapter(_name, _mapper));

        /// <inheritdoc/>
        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new EventLogStreamFailureHandler(queueId));

        /// <inheritdoc/>
        public IQueueAdapterCache GetQueueAdapterCache() => _adapterCache;

        /// <inheritdoc/>
        public IStreamQueueMapper GetStreamQueueMapper() => _mapper;

        /// <summary>
        /// Factory method for creating the factory.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to use for creation.</param>
        /// <param name="name">Name of the factory.</param>
        /// <returns>An <see cref="EventLogQueueAdapterFactory"/> instance.</returns>
        public static EventLogQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
        {
            return new(name, serviceProvider.GetService<ILoggerFactory>()!);
        }
    }
}
