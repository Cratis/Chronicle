// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInversion;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.Streams;
using Orleans.Streams.Core;

namespace Cratis.Events.Store.MongoDB
{
    public class EventLogQueueAdapterFactory : IQueueAdapterFactory
    {
        readonly IQueueAdapterCache _cache;
        readonly IStreamQueueMapper _mapper;
        readonly string _name;
        readonly ProviderFor<IEventLogs> _eventLogsProvder;

        public EventLogQueueAdapterFactory(
            string name,
            ProviderFor<IEventLogs> eventLogsProvder,
            IExecutionContextManager executionContextManager,
            ProviderFor<IMongoDatabase> mongoDatabaseProvider)
        {
            _mapper = new HashRingBasedStreamQueueMapper(new(), name);
            _cache = new EventLogQueueAdapterCache(executionContextManager, mongoDatabaseProvider);
            _name = name;
            _eventLogsProvder = eventLogsProvder;
        }

        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventLogQueueAdapter(_name, _mapper, _eventLogsProvder));

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());

        public IQueueAdapterCache GetQueueAdapterCache() => _cache;

        public IStreamQueueMapper GetStreamQueueMapper() => _mapper;

        public static EventLogQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
        {
            return new(
                name,
                serviceProvider.GetService<ProviderFor<IEventLogs>>()!,
                serviceProvider.GetService<IExecutionContextManager>()!,
                serviceProvider.GetService<ProviderFor<IMongoDatabase>>()!);
        }
    }
}
