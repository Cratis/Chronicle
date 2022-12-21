// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Collections;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Observation;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Grains.Connections;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Extensions.Orleans.Configuration;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;
using OrleansClientBuilder = Orleans.ClientBuilder;

namespace Aksio.Cratis.Hosting;

/// <summary>
/// Represents an implementation of <see cref="IClientBuilder"/>.
/// </summary>
public class ClientBuilder : IClientBuilder
{
    readonly MicroserviceId _microserviceId;
    bool _inKernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
    /// </summary>
    /// <param name="microserviceId">Microservice identifier.</param>
    public ClientBuilder(MicroserviceId microserviceId)
    {
        ExecutionContextManager.SetGlobalMicroserviceId(microserviceId);
        _microserviceId = microserviceId;
    }

    /// <summary>
    /// Start configuring <see cref="IClientBuilder"/> for a specific <see cref="MicroserviceId"/>.
    /// </summary>
    /// <param name="id"><see cref="MicroserviceId"/>.</param>
    /// <returns><see cref="IClientBuilder"/> to build.</returns>
    public static IClientBuilder ForMicroservice(MicroserviceId id)
    {
        return new ClientBuilder(id);
    }

    /// <inheritdoc/>
    public IClientBuilder InKernel()
    {
        ExecutionContextManager.SetKernelMode();
        _inKernel = true;
        return this;
    }

    /// <inheritdoc/>
    public void Build(
        HostBuilderContext hostBuilderContext,
        IServiceCollection services,
        ITypes? types = default,
        ILoggerFactory? loggerFactory = default)
    {
        var logger = loggerFactory?.CreateLogger<ClientBuilder>();
        logger?.Configuring();

        if (types == default)
        {
            types = new Types.Types();
        }

        services
            .AddMongoDBReadModels(types, loggerFactory: loggerFactory)
            .AddTransient(sp => sp.GetService<IEventStore>()!.EventLog)
            .AddTransient(sp => sp.GetService<IEventStore>()!.Outbox);

        if (_inKernel)
        {
            return;
        }

        logger?.ConfiguringServices();
        var connectionManager = new ConnectionManager();
        services
            .AddSingleton<IConnectionManager>(connectionManager)
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>))
            .AddTransient(typeof(IImplementationsOf<>), typeof(ImplementationsOf<>))
            .AddTransient<IEventStore, EventStore>()
            .AddSingleton(types)
            .AddSingleton<IProjectionsRegistrar, ProjectionsRegistrar>()
            .AddProjections()
            .AddOutboxProjections()
            .AddIntegration()
            .AddSingleton<IObservers, Observers>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
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

        logger?.ConfiguringCompliance();

        types.All.Where(_ =>
            _ != typeof(ICanProvideComplianceMetadataForType) &&
            _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ForEach(_ => services.AddTransient(_));
        types.All.Where(_ =>
            _ != typeof(ICanProvideComplianceMetadataForProperty) &&
            _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ForEach(_ => services.AddTransient(_));

        services.AddSingleton(sp =>
        {
            logger?.ConfiguringKernelConnection();
            var orleansBuilder = new SiloHostBuilder()
                .UseCluster(services.GetClusterConfig(), _microserviceId, logger)
                .UseExecutionContext()
                .UseConnectionIdFromConnectionContextForOutgoingCalls()
                .ConfigureServices(services => services
                    .AddSingleton<IConnectionManager>(connectionManager)
                    .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
                    .AddSingleton<IRequestContextManager, RequestContextManager>()
                    .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>());

            var orleansClient = orleansBuilder.Build();

            logger?.ConnectingToKernel();
            orleansClient.StartAsync().Wait();
            logger?.ConnectedToKernel();

            var client = orleansClient.Services.GetRequiredService<IClusterClient>();

#pragma warning disable CA2008
            client
                .GetGrain<IConnectedClients>(Guid.Empty)
                .GetLastConnectedClientConnectionId().ContinueWith(_ => ConnectionManager.InternalConnectionId = _.Result);

            return orleansClient;
        });
    }
}
