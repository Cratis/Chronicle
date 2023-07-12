// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Models;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
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
    /// <param name="microserviceName">The <see cref="MicroserviceName"/> for the microservice.</param>
    public ClientBuilder(MicroserviceId microserviceId, MicroserviceName microserviceName)
    {
        ExecutionContextManager.SetGlobalMicroserviceId(microserviceId);
        ExecutionContextManager.SetGlobalMicroserviceName(microserviceName);
    }

    /// <summary>
    /// Start configuring <see cref="IClientBuilder"/> for a specific <see cref="MicroserviceId"/>.
    /// </summary>
    /// <param name="id">The <see cref="MicroserviceId"/>.</param>
    /// <param name="name">The <see cref="MicroserviceName"/>.</param>
    /// <returns><see cref="IClientBuilder"/> to build.</returns>
    public static IClientBuilder ForMicroservice(MicroserviceId id, MicroserviceName name)
    {
        return new ClientBuilder(id, name);
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
        #pragma warning disable CA2000 // Dispose objects before losing scope - Logger factory will be disposed when process exits
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ClientBuilder>()!;
        logger.Configuring();

        clientArtifacts ??= new DefaultClientArtifactsProvider(ProjectReferencedAssemblies.Instance);

        services
            .AddHttpClient()
            .AddSingleton(clientArtifacts)
            .AddSingleton(_modelNameConvention ?? new DefaultModelNameConvention())
            .AddTransient(sp => sp.GetService<IEventStore>()!.EventLog)
            .AddTransient(sp => sp.GetService<IEventStore>()!.Outbox)
            .AddObservers(clientArtifacts);

        if (_inKernel)
        {
            services.AddCratisInsideSiloClient();
            return;
        }

        logger.ConfiguringServices();
        services
            .AddCratisClient()
            .AddTransient<IEventStore, EventStore>()
            .AddSingleton<IClientLifecycle, ClientLifecycle>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<IExecutionContextManager, ExecutionContextManager>();

        logger.ConfiguringCompliance();

        clientArtifacts.ComplianceForTypesProviders.ForEach(_ => services.AddTransient(_));
        clientArtifacts.ComplianceForPropertiesProviders.ForEach(_ => services.AddTransient(_));
    }
}
