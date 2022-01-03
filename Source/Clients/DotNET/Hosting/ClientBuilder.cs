// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Cratis.Compliance;
using Cratis.Events;
using Cratis.Events.Observation;
using Cratis.Events.Schemas;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Extensions.Orleans.Execution;
using Cratis.Schemas;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;
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

        /// <inheritdoc/>
        public static IClientBuilder ForMicroservice(MicroserviceId id)
        {
            return new ClientBuilder(id);
        }

        /// <inheritdoc/>
        public void Build(HostBuilderContext hostBuilderContext, IServiceCollection services, ITypes? types = default)
        {
            if (types == default)
            {
                types = new Types.Types();
            }

            services
                .AddSingleton(types)
                .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>))
                .AddTransient(typeof(IImplementationsOf<>), typeof(ImplementationsOf<>))
                .AddSingleton<IEventStore, EventStore>()
                .AddSingleton<IObservers, Observers>()
                .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
                .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
                .AddSingleton<ISchemas, Events.Schemas.Schemas>()
                .AddSingleton<IEventTypes, EventTypes>()
                .AddSingleton<IEventSerializer, EventSerializer>()
                .AddSingleton<IHostedService, ObserversService>()
                .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
                .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>()
                .AddSingleton<IRequestContextManager, RequestContextManager>();
            types.AllObservers().ForEach(_ => services.AddTransient(_));

            types.All.Where(_ =>
                _ != typeof(ICanProvideComplianceMetadataForType) &&
                _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ForEach(_ => services.AddTransient(_));
            types.All.Where(_ =>
                _ != typeof(ICanProvideComplianceMetadataForProperty) &&
                _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ForEach(_ => services.AddTransient(_));

            var orleansBuilder = new OrleansClientBuilder()
                .UseLocalhostClustering()
                // TODO: .AddClusterConnectionLostHandler()
                .AddEventLogStream()
                .AddSimpleMessageStreamProvider("observer-handlers")
                .UseExecutionContext()
                .ConfigureServices(services => services
                    .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
                    .AddSingleton<IRequestContextManager, RequestContextManager>()
                    .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>());

            var orleansClient = orleansBuilder.Build();

            services.AddSingleton(orleansClient);

            orleansClient.Connect(async (_) =>
            {
                await Task.Delay(1000);
                return true;
            }).Wait();
        }
    }
}
