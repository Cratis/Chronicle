// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Collections;
using Aksio.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Models;
using Aksio.Cratis.Observation;
using Aksio.Schemas;
using Aksio.Types;
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
        IClientArtifactsProvider? clientArtifacts = default,
        ILoggerFactory? loggerFactory = default)
    {
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ClientBuilder>()!;
        logger.Configuring();

        clientArtifacts ??= new DefaultClientArtifactsProvider(ProjectReferencedAssemblies.Instance);

        services
            .AddHttpClient()
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
            .AddTransient<IEventStore, EventStore>()
            .AddSingleton(clientArtifacts)
            .AddSingleton<IClientLifecycle, ClientLifecycle>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
            .AddObservers(clientArtifacts);

        logger.ConfiguringCompliance();

        clientArtifacts.ComplianceForTypesProviders.ForEach(_ => services.AddTransient(_));
        clientArtifacts.ComplianceForPropertiesProviders.ForEach(_ => services.AddTransient(_));
    }
}
