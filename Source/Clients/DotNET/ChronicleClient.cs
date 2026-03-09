// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Json;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IChronicleClient"/>.
/// </summary>
public class ChronicleClient : IChronicleClient, IDisposable
{
    const string VersionMetadataKey = "softwareVersion";
    const string CommitMetadataKey = "softwareCommit";
    const string DotNetClientVersionMetadataKey = ".NET Client Version";
    const string DotNetClientCommitMetadataKey = ".NET Client Commit";
    const string ProgramIdentifierMetadataKey = "programIdentifier";
    const string OperatingSystemMetadataKey = "os";
    const string MachineNameMetadataKey = "machineName";
    const string ProcessMetadataKey = "process";

    readonly IChronicleConnection _connection;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    readonly IClientArtifactsActivator _artifactActivator;
    readonly IClientArtifactsProvider _artifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly IIdentityProvider _identityProvider;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly IEventStoreNamespaceResolver _namespaceResolver;
    readonly ILoggerFactory _loggerFactory;
    readonly ConcurrentDictionary<EventStoreKey, IEventStore> _eventStores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <remarks>
    /// This initializes the client with the development connection string
    /// (<see cref="ChronicleConnectionString.Development" />), which includes the default development
    /// client credentials.
    /// </remarks>
    public ChronicleClient()
        : this(ChronicleConnectionString.Development)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string to use.</param>
    public ChronicleClient(string connectionString)
        : this(new ChronicleConnectionString(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connectionString"><see cref="ChronicleConnectionString"/> to connect with.</param>
    public ChronicleClient(ChronicleConnectionString connectionString)
        : this(ChronicleOptions.FromConnectionString(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. Falls back to <see cref="ChronicleOptions.ArtifactsProvider"/> if not provided.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/>. Falls back to <see cref="ChronicleOptions.ServiceProvider"/> if not provided.</param>
    /// <param name="identityProvider">Optional <see cref="IIdentityProvider"/>. Falls back to <see cref="ChronicleOptions.IdentityProvider"/> if not provided.</param>
    /// <param name="correlationIdAccessor">Optional <see cref="ICorrelationIdAccessor"/>. Falls back to <see cref="ChronicleOptions.CorrelationIdAccessor"/> if not provided.</param>
    /// <param name="namespaceResolver">Optional <see cref="IEventStoreNamespaceResolver"/>. Falls back to <see cref="ChronicleOptions.EventStoreNamespaceResolver"/> if not provided.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>. Falls back to <see cref="ChronicleOptions.LoggerFactory"/> if not provided.</param>
#pragma warning disable CS0618
    public ChronicleClient(
        ChronicleOptions options,
        IClientArtifactsProvider? artifactsProvider = null,
        IServiceProvider? serviceProvider = null,
        IIdentityProvider? identityProvider = null,
        ICorrelationIdAccessor? correlationIdAccessor = null,
        IEventStoreNamespaceResolver? namespaceResolver = null,
        ILoggerFactory? loggerFactory = null)
    {
        Options = options;
        _artifactsProvider = artifactsProvider ?? options.ArtifactsProvider;
        _serviceProvider = serviceProvider ?? options.ServiceProvider;
        _identityProvider = identityProvider ?? options.IdentityProvider;
        _correlationIdAccessor = correlationIdAccessor ?? options.CorrelationIdAccessor;
        _namespaceResolver = namespaceResolver ?? options.EventStoreNamespaceResolver;
        _loggerFactory = loggerFactory ?? options.LoggerFactory;
#pragma warning restore CS0618

        var result = InitializeInternal();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;
        _artifactActivator = result.ArtifactActivator;

        var tokenProvider = CreateTokenProvider(options);
        var connectionLifecycle = new ConnectionLifecycle(_loggerFactory.CreateLogger<ConnectionLifecycle>());

        var certificatePath = options.Tls.CertificatePath ?? options.ConnectionString.CertificatePath;
        var certificatePassword = options.Tls.CertificatePassword ?? options.ConnectionString.CertificatePassword;
        var disableTls = options.Tls.IsDisabled || (string.IsNullOrEmpty(certificatePath) && options.ConnectionString.DisableTls);

        _connection = new ChronicleConnection(
            options.ConnectionString,
            options.ConnectTimeout,
            options.MaxReceiveMessageSize,
            options.MaxSendMessageSize,
            connectionLifecycle,
            new Tasks.TaskFactory(),
            _correlationIdAccessor,
            _loggerFactory,
            CancellationToken.None,
            _loggerFactory.CreateLogger<ChronicleConnection>(),
            disableTls,
            certificatePath,
            certificatePassword,
            tokenProvider);
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IChronicleConnection"/> to use.</param>
    /// <param name="options">Optional <see cref="ChronicleOptions"/>.</param>
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. Falls back to <see cref="ChronicleOptions.ArtifactsProvider"/> if not provided.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/>. Falls back to <see cref="ChronicleOptions.ServiceProvider"/> if not provided.</param>
    /// <param name="identityProvider">Optional <see cref="IIdentityProvider"/>. Falls back to <see cref="ChronicleOptions.IdentityProvider"/> if not provided.</param>
    /// <param name="correlationIdAccessor">Optional <see cref="ICorrelationIdAccessor"/>. Falls back to <see cref="ChronicleOptions.CorrelationIdAccessor"/> if not provided.</param>
    /// <param name="namespaceResolver">Optional <see cref="IEventStoreNamespaceResolver"/>. Falls back to <see cref="ChronicleOptions.EventStoreNamespaceResolver"/> if not provided.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>. Falls back to <see cref="ChronicleOptions.LoggerFactory"/> if not provided.</param>
#pragma warning disable CS0618
    public ChronicleClient(
        IChronicleConnection connection,
        ChronicleOptions options,
        IClientArtifactsProvider? artifactsProvider = null,
        IServiceProvider? serviceProvider = null,
        IIdentityProvider? identityProvider = null,
        ICorrelationIdAccessor? correlationIdAccessor = null,
        IEventStoreNamespaceResolver? namespaceResolver = null,
        ILoggerFactory? loggerFactory = null)
    {
        Options = options;
        _artifactsProvider = artifactsProvider ?? options.ArtifactsProvider;
        _serviceProvider = serviceProvider ?? options.ServiceProvider;
        _identityProvider = identityProvider ?? options.IdentityProvider;
        _correlationIdAccessor = correlationIdAccessor ?? options.CorrelationIdAccessor;
        _namespaceResolver = namespaceResolver ?? options.EventStoreNamespaceResolver;
        _loggerFactory = loggerFactory ?? options.LoggerFactory;
#pragma warning restore CS0618

        var result = InitializeInternal();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;
        _artifactActivator = result.ArtifactActivator;
        _connection = connection;
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <inheritdoc/>
    public ChronicleOptions Options { get; }

    /// <inheritdoc/>
    public ICausationManager CausationManager { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.Dispose();
    }

    /// <inheritdoc/>
    public async Task<IEventStore> GetEventStore(
        EventStoreName name,
        EventStoreNamespaceName? @namespace = null)
    {
        @namespace ??= _namespaceResolver.Resolve();
        var key = new EventStoreKey(name, @namespace);
        if (_eventStores.TryGetValue(key, out var eventStore))
        {
            return eventStore;
        }

        eventStore = new EventStore(
            name,
            @namespace,
            _connection,
            _artifactsProvider,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            CausationManager,
            _identityProvider,
            _jsonSchemaGenerator,
            Options.NamingPolicy,
            _serviceProvider,
            _artifactActivator,
            Options.AutoDiscoverAndRegister,
            Options.JsonSerializerOptions,
            _loggerFactory);
        _eventStores[key] = eventStore;

        if (Options.AutoDiscoverAndRegister)
        {
            await eventStore.DiscoverAll();
        }

        await _connection.Connect();
        return eventStore;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default)
    {
        var eventStores = await _servicesAccessor.Services.EventStores.GetEventStores();
        return eventStores.Select(_ => (EventStoreName)_).ToArray();
    }

    (ICausationManager CausationManager, IJsonSchemaGenerator JsonSchemaGenerator, IConcurrencyScopeStrategies ConcurrencyScopeStrategies, IClientArtifactsActivator ArtifactActivator) InitializeInternal()
    {
        var causationManager = new CausationManager();
        causationManager.DefineRoot(new Dictionary<string, string>
        {
            { VersionMetadataKey, Options.SoftwareVersion },
            { CommitMetadataKey, Options.SoftwareCommit },
            { DotNetClientVersionMetadataKey, VersionInformation.GetChronicleClientVersion() },
            { DotNetClientCommitMetadataKey, VersionInformation.GetChronicleClientCommitSha() },
            { ProgramIdentifierMetadataKey, Options.ProgramIdentifier },
            { OperatingSystemMetadataKey, Environment.OSVersion.ToString() },
            { MachineNameMetadataKey, Environment.MachineName },
            { ProcessMetadataKey, Environment.ProcessPath ?? string.Empty }
        });

        var complianceMetadataResolver = new ComplianceMetadataResolver(
            new InstancesOf<ICanProvideComplianceMetadataForType>(Types.Types.Instance, _serviceProvider),
            new InstancesOf<ICanProvideComplianceMetadataForProperty>(Types.Types.Instance, _serviceProvider));
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver, Options.NamingPolicy);
        var concurrencyScopeStrategies = new ConcurrencyScopeStrategies(Options.ConcurrencyOptions, _serviceProvider);
        var artifactActivator = new ClientArtifactsActivator(_serviceProvider, _loggerFactory);

        InitializeJsonSerializationOptions();

        return (causationManager, jsonSchemaGenerator, concurrencyScopeStrategies, artifactActivator);
    }

    ITokenProvider CreateTokenProvider(ChronicleOptions options)
    {
        if (options.ConnectionString.AuthenticationMode == AuthenticationMode.ClientCredentials)
        {
            return new OAuthTokenProvider(
                options.ConnectionString.ServerAddress,
                options.ConnectionString.Username ?? string.Empty,
                options.ConnectionString.Password ?? string.Empty,
                options.ManagementPort,
                options.Tls.IsDisabled,
                _loggerFactory.CreateLogger<OAuthTokenProvider>());
        }

        return new NoOpTokenProvider();
    }

    void InitializeJsonSerializationOptions()
    {
        Options.JsonSerializerOptions = new JsonSerializerOptions(Options.JsonSerializerOptions)
        {
            PropertyNamingPolicy = Options.NamingPolicy.JsonPropertyNamingPolicy,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        Options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new ConceptAsJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new TypeJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new UriJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());
    }

    record EventStoreKey(EventStoreName Name, EventStoreNamespaceName Namespace);
}
