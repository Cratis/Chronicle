// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Cratis.Events.Store.Orleans.StreamProvider
{
    public class EventLogQueueAdapterFactory : IQueueAdapterFactory
    {
        readonly IQueueAdapterCache _cache;
        readonly IStreamQueueMapper _mapper;
        readonly string _name;

        public EventLogQueueAdapterFactory(string name, ILoggerFactory loggerFactory)
        {
            _mapper = new HashRingBasedStreamQueueMapper(new(), name);
            _cache = new SimpleQueueAdapterCache(new(), name, loggerFactory);
            _name = name;
        }

        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventLogQueueAdapter(_name, _mapper));

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

        public IQueueAdapterCache GetQueueAdapterCache() => _cache;

        public IStreamQueueMapper GetStreamQueueMapper() => _mapper;

        public static EventLogQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
        {
            return new(name, serviceProvider.GetService<ILoggerFactory>()!);
        }
    }
}
