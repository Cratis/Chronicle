// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Configuration;
using Aksio.Cratis.Client;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences.Outbox;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Models;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Schemas;
using Aksio.Json;
using Aksio.Tasks;
using Aksio.Timers;
using Aksio.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFactory = Aksio.Tasks.TaskFactory;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="IClientBuilder"/>.
/// </summary>
public class ClientBuilder : IClientBuilder
{
#pragma warning disable SA1600, CA1051
    protected readonly ILogger<ClientBuilder> _logger;
    protected IClientArtifactsProvider? _clientArtifactsProvider;
    protected IModelNameConvention? _modelNameConvention;
    readonly OptionsBuilder<ClientOptions> _optionsBuilder;
    bool _inKernel;
    bool _isMultiTenanted;

    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to register services with.</param>
    /// <param name="logger">Logger for logging.</param>
    public ClientBuilder(
        IServiceCollection services,
        ILogger<ClientBuilder> logger)
    {
        _optionsBuilder = services.AddOptions<ClientOptions>();
        SetDefaultOptions();
        _optionsBuilder.BindConfiguration("cratis")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        Services = services;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IClientBuilder ForMicroservice(MicroserviceId microserviceId, MicroserviceName microserviceName)
    {
        _optionsBuilder.Configure(options =>
        {
            options.MicroserviceId = microserviceId;
            options.MicroserviceName = microserviceName;
        });
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder MultiTenanted()
    {
        _isMultiTenanted = true;
        Services.Configure<ClientOptions>(options => options.IsMultiTenanted = true);
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder InKernel()
    {
        ExecutionContextManager.SetGlobalMicroserviceId(MicroserviceId.Kernel);
        ExecutionContextManager.SetGlobalMicroserviceName("Cratis Kernel");
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
    public IClientBuilder UseClientArtifacts(IClientArtifactsProvider clientArtifactsProvider)
    {
        _clientArtifactsProvider = clientArtifactsProvider;
        return this;
    }

    /// <inheritdoc/>
    public void Build()
    {
        _logger.Configuring();

        var options = Services.BuildServiceProvider().GetRequiredService<IOptions<ClientOptions>>();

        var clientArtifacts = _clientArtifactsProvider ?? new DefaultClientArtifactsProvider(ProjectReferencedAssemblies.Instance);

        _logger.ConfiguringServices();

        Services
            .AddHttpClient()
            .AddSingleton(clientArtifacts)
            .AddSingleton(_modelNameConvention ?? new DefaultModelNameConvention())
            .AddObservers(clientArtifacts)
            .AddSingleton(Globals.JsonSerializerOptions)
            .AddSingleton<IConnectionLifecycle, ConnectionLifecycle>()
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
            .AddSingleton<OutboxProjectionsRegistrar>()
            .AddSingleton<AdaptersConnectionLifecycleParticipant>()
            .AddSingleton<ObserversConnectionLifecycleParticipant>()
            .AddSingleton<ProjectionsRegistrar>()
            .AddSingleton<SchemasConnectionLifecycleParticipant>()
            .AddSingleton<IJsonProjectionSerializer, JsonProjectionSerializer>()
            .AddSingleton<IAdapters, Adapters>()
            .AddSingleton<IAdapterProjectionFactory, AdapterProjectionFactory>()
            .AddSingleton<IAdapterMapperFactory, AdapterMapperFactory>()
            .AddSingleton<IImmediateProjections, ImmediateProjections>()
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>));

        _logger.ConfiguringCompliance();

        clientArtifacts.ComplianceForTypesProviders.ForEach(_ => Services.AddTransient(_));
        clientArtifacts.ComplianceForPropertiesProviders.ForEach(_ => Services.AddTransient(_));

        if (_inKernel)
        {
            _logger.UsingInsideKernelClient();
            ForMicroservice(MicroserviceId.Kernel, "Cratis Kernel");
            ExecutionContextManager.SetKernelMode();
            Services.AddSingleton<IConnection, InsideKernelConnection>();
        }
        else if (options.Value.Kernel.SingleKernelOptions is not null)
        {
            _logger.UsingSingleKernelClient(options.Value.Kernel.SingleKernelOptions.Endpoint);
            Services.AddSingleton<IConnection, SingleKernelConnection>();
        }
        else if (options.Value.Kernel.StaticClusterOptions is not null)
        {
            _logger.UsingStaticClusterKernelClient();
            Services.AddSingleton<IConnection, StaticClusteredKernelConnection>();
        }
        else if (options.Value.Kernel.AzureStorageClusterOptions is not null)
        {
            _logger.UsingOrleansAzureStorageKernelClient();
            Services.AddSingleton<IConnection, OrleansAzureTableStoreKernelConnection>();
        }

        if (_isMultiTenanted)
        {
            Services.AddSingleton<IMultiTenantEventSequences, MultiTenantEventSequences>();
            Services.AddSingleton<IMultiTenantEventStore, MultiTenantEventStore>();
            Services.AddSingleton<IClient, MultiTenantClient>();
            Services.AddTransient(sp =>
            {
                var tenantId = ExecutionContextManager.GetCurrent().TenantId;
                return sp.GetRequiredService<IMultiTenantEventSequences>().ForTenant(tenantId).EventLog;
            });
            Services.AddTransient(sp =>
            {
                var tenantId = ExecutionContextManager.GetCurrent().TenantId;
                return sp.GetRequiredService<IMultiTenantEventSequences>().ForTenant(tenantId).Outbox;
            });
        }
        else
        {
            Services.AddSingleton(TenantId.NotSet);
            Services.AddSingleton<IEventSequences, Client.EventSequences>();
            Services.AddSingleton<ISingleTenantEventStore, SingleTenantEventStore>();
            Services.AddSingleton<IClient, SingleTenantClient>();
            Services.AddSingleton(sp => sp.GetRequiredService<IEventSequences>().EventLog);
            Services.AddSingleton(sp => sp.GetRequiredService<IEventSequences>().Outbox);
        }
    }

    void SetDefaultOptions()
    {
        _optionsBuilder
            .Configure(options =>
            {
                options.MicroserviceId = MicroserviceId.Unspecified;
                options.MicroserviceName = MicroserviceName.Unspecified;
                options.Kernel = new()
                {
                    SingleKernelOptions = new()
                };
            });
    }
}
