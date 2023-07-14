// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Cratis.Client;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.Models;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
using Aksio.Json;
using Aksio.Tasks;
using Aksio.Timers;
using Aksio.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFactory = Aksio.Tasks.TaskFactory;

namespace Aksio.Cratis;

/// <summary>
/// Represents the starting point for creating a Cratis client.
/// </summary>
#pragma warning disable RCS1102, CA1052
public class ClientBuilder
{
    /// <summary>
    /// Start configuring <see cref="ISingleTenantClientBuilder"/> for a single tenanted client.
    /// </summary>
    /// <returns><see cref="ISingleTenantClientBuilder"/> to build.</returns>
    public static ISingleTenantClientBuilder SingleTenanted() => new SingleTenantClientBuilder();

    /// <summary>
    /// Start configuring <see cref="IMultiTenantClientBuilder"/> for a single tenanted client.
    /// </summary>
    /// <returns><see cref="ISingleTenantClientBuilder"/> to build.</returns>
    public static IMultiTenantClientBuilder MultiTenanted() => new MultiTenantClientBuilder();
}

/// <summary>
/// Represents an implementation of <see cref="IClientBuilder{TActual, TClient}"/>.
/// </summary>
/// <typeparam name="TActual">Type of the actual client builder.</typeparam>
/// <typeparam name="TClient">Type of client it builds.</typeparam>
public abstract class ClientBuilder<TActual, TClient> : IClientBuilder<TActual, TClient>
    where TActual : class, IClientBuilder<TActual, TClient>
{
#pragma warning disable SA1600, CA1051
    protected IModelNameConvention? _modelNameConvention;
    protected MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    protected MicroserviceName _microserviceName = MicroserviceName.Unspecified;
    bool _inKernel;

    /// <inheritdoc/>
    public TActual ForMicroservice(MicroserviceId microserviceId, MicroserviceName microserviceName)
    {
        _microserviceId = microserviceId;
        _microserviceName = microserviceName;
        return (this as TActual)!;
    }

    /// <inheritdoc/>
    public TActual InKernel()
    {
        ExecutionContextManager.SetKernelMode();
        _inKernel = true;
        return (this as TActual)!;
    }

    /// <inheritdoc/>
    public TActual UseModelNameConvention(IModelNameConvention convention)
    {
        _modelNameConvention = convention;
        return (this as TActual)!;
    }

    /// <inheritdoc/>
    public TClient Build(
        IServiceCollection? services = default,
        IClientArtifactsProvider? clientArtifacts = default,
        ILoggerFactory? loggerFactory = default)
    {
        services ??= new ServiceCollection();

#pragma warning disable CA2000 // Dispose objects before losing scope - Logger factory will be disposed when process exits
        loggerFactory ??= LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ClientBuilder>()!;
        logger.Configuring();

        clientArtifacts ??= new DefaultClientArtifactsProvider(ProjectReferencedAssemblies.Instance);

        logger.ConfiguringServices();

        services
            .AddHttpClient()
            .AddSingleton(clientArtifacts)
            .AddSingleton(_modelNameConvention ?? new DefaultModelNameConvention())
            .AddObservers(clientArtifacts)
            .AddSingleton(Globals.JsonSerializerOptions)
            .AddSingleton<IClientLifecycle, ClientLifecycle>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
            .AddSingleton<IObserversRegistrar, ObserversRegistrar>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
            .AddSingleton<ITypes>(Types.Types.Instance)
            .AddSingleton<ITaskFactory, TaskFactory>()
            .AddSingleton<ITimerFactory, TimerFactory>()
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>));

        logger.ConfiguringCompliance();

        clientArtifacts.ComplianceForTypesProviders.ForEach(_ => services.AddTransient(_));
        clientArtifacts.ComplianceForPropertiesProviders.ForEach(_ => services.AddTransient(_));

        if (_inKernel)
        {
            services.AddCratisInsideSiloClient();
        }
        else
        {
            services.AddCratisClient();
        }

        return BuildActual(services, clientArtifacts, loggerFactory);
    }

    protected abstract TClient BuildActual(IServiceCollection services, IClientArtifactsProvider clientArtifacts, ILoggerFactory loggerFactory);
}
