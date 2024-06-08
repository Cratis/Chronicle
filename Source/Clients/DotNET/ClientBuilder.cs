// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Compliance.GDPR;
using Aksio.Cratis.Models;
using Aksio.Cratis.Net;
using Aksio.Cratis.Schemas;
using Aksio.Json;
using Aksio.Tasks;
using Aksio.Timers;
using Aksio.Types;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Client;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Outbox;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFactory = Aksio.Tasks.TaskFactory;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IClientBuilder"/>.
/// </summary>
public class ClientBuilder : IClientBuilder
{
#pragma warning disable SA1600, CA1051
    protected readonly ILogger<ClientBuilder> _logger;
    protected IClientArtifactsProvider? _clientArtifactsProvider;
    protected IModelNameConvention? _modelNameConvention;

    const string VersionMetadataKey = "softwareVersion";
    const string CommitMetadataKey = "softwareCommit";
    const string ProgramIdentifierMetadataKey = "programIdentifier";

    readonly OptionsBuilder<ClientOptions> _optionsBuilder;
    readonly Dictionary<string, string> _metadata = new();
    bool _inKernel;
    bool _isMultiTenanted;
    Type _identityProviderType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientBuilder"/> class.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to register services with.</param>
    /// <param name="logger">Logger for logging.</param>
    public ClientBuilder(
        IServiceCollection services,
        ILogger<ClientBuilder> logger)
    {
        _metadata[VersionMetadataKey] = "0.0.0";
        _metadata[CommitMetadataKey] = "[N/A]";
        _metadata[ProgramIdentifierMetadataKey] = "[N/A]";
        _metadata["os"] = Environment.OSVersion.ToString();
        _metadata["machineName"] = Environment.MachineName;
        _metadata["process"] = Environment.ProcessPath ?? string.Empty;
        _identityProviderType = typeof(BaseIdentityProvider);

        _optionsBuilder = services.AddOptions<ClientOptions>();
        SetDefaultOptions();
        _optionsBuilder.BindConfiguration("cratis")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        Services = services;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    /// <inheritdoc/>
    public IClientBuilder ForMicroservice(MicroserviceId microserviceId, MicroserviceName microserviceName)
    {
        ExecutionContextManager.SetGlobalMicroserviceId(microserviceId);
        ExecutionContextManager.SetGlobalMicroserviceName(microserviceName);
        ExecutionContextManager.SetCurrent(new ExecutionContext(microserviceId, TenantId.NotSet, CorrelationId.New(), _inKernel));

        _optionsBuilder.Configure(options =>
        {
            options.MicroserviceId = microserviceId;
            options.MicroserviceName = microserviceName;
        });
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder WithSoftwareVersion(string version, string commit)
    {
        _metadata[VersionMetadataKey] = version;
        _metadata[CommitMetadataKey] = commit;
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder IdentifiedAs(string name)
    {
        _metadata[ProgramIdentifierMetadataKey] = name;
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
        _inKernel = true;
        ForMicroservice(MicroserviceId.Kernel, "Cratis Kernel");
        return this;
    }

    /// <inheritdoc/>
    public IClientBuilder UseIdentityProvider<T>()
        where T : IIdentityProvider
    {
        _identityProviderType = typeof(T);
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

        var clientArtifacts = _clientArtifactsProvider ?? new DefaultClientArtifactsProvider(
            new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

        _logger.ConfiguringServices();

        CausationManager.DefineRoot(_metadata);

        Services
            .AddHttpClient()
            .AddSingleton(clientArtifacts)
            .AddSingleton(_modelNameConvention ?? new DefaultModelNameConvention())
            .AddSingleton<IModelNameResolver, ModelNameResolver>()
            .AddObservers(clientArtifacts)
            .AddSingleton(Globals.JsonSerializerOptions)
            .AddSingleton<IConnectionLifecycle, ConnectionLifecycle>()
            .AddSingleton<IObserverMiddlewares, ObserverMiddlewares>()
            .AddSingleton<IObserversRegistrar, ObserversRegistrar>()
            .AddTransient<IClientObservers, ClientObservers>()
            .AddSingleton<IReducersRegistrar, ReducersRegistrar>()
            .AddSingleton<IReducerValidator, ReducerValidator>()
            .AddTransient<IClientReducers, ClientReducers>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, Events.EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<IExecutionContextManager, ExecutionContextManager>()
            .AddSingleton<ITypes>(Types.Instance)
            .AddSingleton<ITaskFactory, TaskFactory>()
            .AddSingleton<ITimerFactory, TimerFactory>()
            .AddSingleton<OutboxProjectionsRegistrar>()
            .AddSingleton<ObserversConnectionLifecycleParticipant>()
            .AddSingleton<SchemasConnectionLifecycleParticipant>()
            .AddSingleton<RegistrarsConnectionLifecycleParticipant>()
            .AddSingleton<ProjectionsRegistrar>()
            .AddSingleton<IJsonProjectionSerializer, JsonProjectionSerializer>()
            .AddSingleton<IAdapters, Adapters>()
            .AddSingleton<IAdapterProjectionFactory, AdapterProjectionFactory>()
            .AddSingleton<IAdapterMapperFactory, AdapterMapperFactory>()
            .AddSingleton<IProjections, Projections.Projections>()
            .AddSingleton<IImmediateProjections, ImmediateProjections>()
            .AddSingleton<ILoadBalancerStrategy, RoundRobinLoadBalancerStrategy>()
            .AddSingleton<ILoadBalancedHttpClientFactory, LoadBalancedHttpClientFactory>()
            .AddSingleton<ITenantConfiguration, TenantConfiguration>()
            .AddSingleton<IProjections, Projections.Projections>()
            .AddSingleton<IClientProjections, ClientProjections>()
            .AddSingleton<IRulesProjections, RulesProjections>()
            .AddSingleton<ICausationManager, CausationManager>()
            .AddSingleton<PIIMetadataProvider>()
            .AddSingleton(typeof(IIdentityProvider), _identityProviderType)
            .AddSingleton<IRules, Rules.Rules>()
            .AddSingleton<IAggregateRootFactory, AggregateRootFactory>()
            .AddSingleton<IAggregateRootStateProviders, AggregateRootStateProviders>()
            .AddSingleton<IAggregateRootEventHandlersFactory, AggregateRootEventHandlersFactory>()
            .AddTransient<ClientObservers>()
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
        else if (options.Value.Kernel.AzureStorageCluster is not null)
        {
            _logger.UsingOrleansAzureStorageKernelClient();
            Services.AddSingleton<IConnection, OrleansAzureTableStoreKernelConnection>();
        }
        else if (options.Value.Kernel.StaticCluster is not null)
        {
            _logger.UsingStaticClusterKernelClient();
            Services.AddSingleton<IConnection, StaticClusteredKernelConnection>();
        }
        else if (options.Value.Kernel.SingleKernel is not null)
        {
            _logger.UsingSingleKernelClient(options.Value.Kernel.SingleKernel.Endpoint);
            Services.AddSingleton<IConnection, SingleKernelConnection>();
        }

        if (_isMultiTenanted)
        {
            Services.AddSingleton<IMultiTenantEventSequences, MultiTenantEventSequences>();
            Services.AddSingleton<IMultiTenantEventStore, MultiTenantEventStore>();
            Services.AddSingleton<IClient, MultiTenantClient>();
            Services.AddTransient(sp =>
            {
                var tenantId = ExecutionContextManager.GetCurrent().TenantId;
                return sp.GetRequiredService<IMultiTenantEventSequences>().ForTenant(tenantId);
            });
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
                    SingleKernel = new()
                };
            });
    }
}
