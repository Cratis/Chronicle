// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Net;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Collections;
using Cratis.Json;
using Cratis.Models;
using Cratis.Tasks;
using Cratis.Timers;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskFactory = Cratis.Tasks.TaskFactory;

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

    readonly Dictionary<string, string> _metadata = [];
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
        Services = services;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IServiceCollection Services { get; }

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

        var clientArtifacts = _clientArtifactsProvider ?? new DefaultClientArtifactsProvider(
             new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

        _logger.ConfiguringServices();

        Services
            .AddHttpClient()
            .AddSingleton(clientArtifacts)
            .AddSingleton(_modelNameConvention ?? new DefaultModelNameConvention())
            .AddSingleton<IModelNameResolver, ModelNameResolver>()
            .AddReactions(clientArtifacts)
            .AddSingleton(Globals.JsonSerializerOptions)
            .AddSingleton<IConnectionLifecycle, ConnectionLifecycle>()
            .AddSingleton<IReactionMiddlewares, ReactionMiddlewares>()
            .AddSingleton<IReducers, Reducers.Reducers>()
            .AddSingleton<IReducerValidator, ReducerValidator>()
            .AddTransient<IClientReducers, ClientReducers>()
            .AddSingleton<IComplianceMetadataResolver, ComplianceMetadataResolver>()
            .AddSingleton<IJsonSchemaGenerator, JsonSchemaGenerator>()
            .AddSingleton<IEventTypes, Events.EventTypes>()
            .AddSingleton<IEventSerializer, EventSerializer>()
            .AddSingleton<ITypes>(Types.Types.Instance)
            .AddSingleton<ITaskFactory, TaskFactory>()
            .AddSingleton<ITimerFactory, TimerFactory>()
            .AddSingleton<IAdapters, Adapters>()
            .AddSingleton<IAdapterProjectionFactory, AdapterProjectionFactory>()
            .AddSingleton<IAdapterMapperFactory, AdapterMapperFactory>()
            .AddSingleton<IProjections, Projections.Projections>()
            .AddSingleton<ILoadBalancerStrategy, RoundRobinLoadBalancerStrategy>()
            .AddSingleton<ILoadBalancedHttpClientFactory, LoadBalancedHttpClientFactory>()
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
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>));

        _logger.ConfiguringCompliance();

        clientArtifacts.ComplianceForTypesProviders.ForEach(_ => Services.AddTransient(_));
        clientArtifacts.ComplianceForPropertiesProviders.ForEach(_ => Services.AddTransient(_));
    }
}
