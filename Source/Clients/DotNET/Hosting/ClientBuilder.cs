// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Cratis.Events;
using Cratis.Events.Observation;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using OrleansClientBuilder = Orleans.ClientBuilder;

namespace Cratis.Hosting
{
    /// <summary>
    /// Represents an implementation of <see cref="IClientBuilder"/>.
    /// </summary>
    public class ClientBuilder : IClientBuilder
    {
        readonly MicroserviceId _microserviceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
        /// </summary>
        /// <param name="microserviceId">Microservice identifier.</param>
        public ClientBuilder(MicroserviceId microserviceId)
        {
            _microserviceId = microserviceId;
        }

        public static IClientBuilder ForMicroservice(MicroserviceId id)
        {
            return new ClientBuilder(id);
        }

        public void Build(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            var types = new Types.Types();

            services
                .AddSingleton<Types.ITypes>(types)
                .AddSingleton<IEventStore, EventStore>()
                .AddSingleton<IObservers, Observers>()
                .AddSingleton<IEventTypes, EventTypes>()
                .AddSingleton<IEventSerializer, EventSerializer>()
                .AddSingleton<IHostedService, ObserversService>()
                .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
                .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>()
                .AddSingleton<IRequestContextManager, RequestContextManager>();
            types.AllObservers().ForEach(_ => services.AddTransient(_));

            var orleansBuilder = new OrleansClientBuilder()
                .UseLocalhostClustering()
                .AddEventLogStream()
                .UseExecutionContext()
                .ConfigureServices(services => services
                    .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
                    .AddSingleton<IRequestContextManager, RequestContextManager>()
                    .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>());

            var orleansClient = orleansBuilder.Build();

            services.AddSingleton(orleansClient);

            orleansClient.Connect().Wait();
        }
    }
}
