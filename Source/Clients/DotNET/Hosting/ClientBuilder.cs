// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Collections;
using Aksio.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Models;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Aksio.Cratis.Hosting;

/// <summary>
/// Represents an implementation of <see cref="IClientBuilder"/>.
/// </summary>
public class ClientBuilder : IClientBuilder
{
    bool _inKernel;
    IModelNameConvention? _modelNameConvention;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
    /// </summary>
    /// <param name="microserviceId">Microservice identifier.</param>
    public ClientBuilder(MicroserviceId microserviceId)
    {
        ExecutionContextManager.SetGlobalMicroserviceId(microserviceId);
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
    public IClientBuilder UseModelNameConvention(IModelNameConvention convention)
    {
        _modelNameConvention = convention;
        return this;
    }

    /// <inheritdoc/>
    public void Build(
        HostBuilderContext hostBuilderContext,
        IServiceCollection services,
        ITypes? types = default,
        ILoggerFactory? loggerFactory = default)
    {
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ClientBuilder>()!;
        logger.Configuring();

        if (types == default)
        {
            types = new Types.Types();
        }

        services
            .AddHttpClient()
            .AddMongoDBReadModels(types, loggerFactory: loggerFactory, modelNameConvention: _modelNameConvention)
            .AddTransient(sp => sp.GetService<IEventStore>()!.EventLog)
            .AddTransient(sp => sp.GetService<IEventStore>()!.Outbox);

        if (_inKernel)
        {
            services.AddCratisInsideSiloClient();
            return;
        }

        logger.ConfiguringServices();
        services
            .AddCratisClient()
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>))
            .AddTransient(typeof(IImplementationsOf<>), typeof(ImplementationsOf<>))
            .AddTransient<IEventStore, EventStore>()
            .AddSingleton(types)
            .AddSingleton<IClientLifecycle, ClientLifecycle>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
            .AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>()
            .AddObservers(types);

        logger.ConfiguringCompliance();

        types.All.Where(_ =>
                    _ != typeof(ICanProvideComplianceMetadataForType) &&
                    _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ForEach(_ => services.AddTransient(_));
        types.All.Where(_ =>
                    _ != typeof(ICanProvideComplianceMetadataForProperty) &&
                    _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ForEach(_ => services.AddTransient(_));
    }
}
