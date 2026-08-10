// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Json;
using Cratis.Serialization;
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
    readonly IEventTypeMigrators _eventTypeMigrators;
    readonly INamingPolicy _namingPolicy;
    readonly CancellationTokenSource? _ownedConnectionCancellation;

    /// <summary>
    /// The event stores handed out, keyed on the name and namespace that identify one.
    /// </summary>
    /// <remarks>
    /// Lazy&lt;Task&lt;T&gt;&gt; with ExecutionAndPublication so that concurrent callers asking for the same event
    /// store share one construction rather than each building their own. Constructing one discovers every client
    /// artifact and connects, so the plain check-then-add this replaced let two callers do all of that twice and
    /// then handed the loser a store that was no longer the cached one.
    /// </remarks>
    readonly ConcurrentDictionary<EventStoreKey, Lazy<Task<IEventStore>>> _eventStores = new();
    int _isDisposed;

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
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. Defaults to <see cref="DefaultClientArtifactsProvider.Default"/> if not provided.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/>. Defaults to <see cref="DefaultServiceProvider"/> if not provided.</param>
    /// <param name="identityProvider">Optional <see cref="IIdentityProvider"/>. Defaults to <see cref="BaseIdentityProvider"/> if not provided.</param>
    /// <param name="correlationIdAccessor">Optional <see cref="ICorrelationIdAccessor"/>. Defaults to <see cref="CorrelationIdAccessor"/> if not provided.</param>
    /// <param name="namespaceResolver">Optional <see cref="IEventStoreNamespaceResolver"/>. Defaults to <see cref="DefaultEventStoreNamespaceResolver"/> if not provided.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>. Defaults to a no-op factory if not provided.</param>
    /// <param name="namingPolicy">Optional <see cref="INamingPolicy"/>. Defaults to <see cref="DefaultNamingPolicy"/> if not provided.</param>
    public ChronicleClient(
        ChronicleOptions options,
        IClientArtifactsProvider? artifactsProvider = null,
        IServiceProvider? serviceProvider = null,
        IIdentityProvider? identityProvider = null,
        ICorrelationIdAccessor? correlationIdAccessor = null,
        IEventStoreNamespaceResolver? namespaceResolver = null,
        ILoggerFactory? loggerFactory = null,
        INamingPolicy? namingPolicy = null)
    {
        Options = options;
        _artifactsProvider = artifactsProvider ?? DefaultClientArtifactsProvider.Default;
        _serviceProvider = serviceProvider ?? new DefaultServiceProvider();
        _identityProvider = identityProvider ?? new BaseIdentityProvider();
        _correlationIdAccessor = correlationIdAccessor ?? new CorrelationIdAccessor();
        _namespaceResolver = namespaceResolver ?? new DefaultEventStoreNamespaceResolver();
        _loggerFactory = loggerFactory ?? new LoggerFactory();
        _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();

        var result = InitializeInternal();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;
        _artifactActivator = result.ArtifactActivator;
        _eventTypeMigrators = new EventTypeMigrators(_artifactsProvider, _serviceProvider);

        var connectionLifecycle = new ConnectionLifecycle(_loggerFactory.CreateLogger<ConnectionLifecycle>());

        var certificatePath = options.Tls.CertificatePath ?? options.ConnectionString.CertificatePath;
        var certificatePassword = options.Tls.CertificatePassword ?? options.ConnectionString.CertificatePassword;

        // The Chronicle server always serves its port over TLS. By default the client validates the
        // server certificate; validation is skipped only when explicitly requested through the TLS
        // options or the connection string (skipTlsValidation=true) — for example to accept the
        // server's self-signed development certificate.
        var skipTlsValidation = options.Tls.SkipCertificateValidation || options.ConnectionString.SkipTlsValidation;

        var tokenProvider = CreateTokenProvider(options, skipTlsValidation);
        _ownedConnectionCancellation = new();

        _connection = new ChronicleConnection(
            options.ConnectionString,
            options.ConnectTimeout,
            options.MaxReceiveMessageSize,
            options.MaxSendMessageSize,
            connectionLifecycle,
            new Tasks.TaskFactory(),
            _correlationIdAccessor,
            _loggerFactory,
            _ownedConnectionCancellation.Token,
            _loggerFactory.CreateLogger<ChronicleConnection>(),
            skipTlsValidation,
            certificatePath,
            certificatePassword,
            tokenProvider,
            skipKeepAlive: options.SkipKeepAlive,
            loadBalancerStrategy: options.LoadBalancerStrategy);
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IChronicleConnection"/> to use.</param>
    /// <param name="options">Optional <see cref="ChronicleOptions"/>.</param>
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. Defaults to <see cref="DefaultClientArtifactsProvider.Default"/> if not provided.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/>. Defaults to <see cref="DefaultServiceProvider"/> if not provided.</param>
    /// <param name="identityProvider">Optional <see cref="IIdentityProvider"/>. Defaults to <see cref="BaseIdentityProvider"/> if not provided.</param>
    /// <param name="correlationIdAccessor">Optional <see cref="ICorrelationIdAccessor"/>. Defaults to <see cref="CorrelationIdAccessor"/> if not provided.</param>
    /// <param name="namespaceResolver">Optional <see cref="IEventStoreNamespaceResolver"/>. Defaults to <see cref="DefaultEventStoreNamespaceResolver"/> if not provided.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>. Defaults to a no-op factory if not provided.</param>
    /// <param name="namingPolicy">Optional <see cref="INamingPolicy"/>. Defaults to <see cref="DefaultNamingPolicy"/> if not provided.</param>
    public ChronicleClient(
        IChronicleConnection connection,
        ChronicleOptions options,
        IClientArtifactsProvider? artifactsProvider = null,
        IServiceProvider? serviceProvider = null,
        IIdentityProvider? identityProvider = null,
        ICorrelationIdAccessor? correlationIdAccessor = null,
        IEventStoreNamespaceResolver? namespaceResolver = null,
        ILoggerFactory? loggerFactory = null,
        INamingPolicy? namingPolicy = null)
    {
        Options = options;
        _artifactsProvider = artifactsProvider ?? DefaultClientArtifactsProvider.Default;
        _serviceProvider = serviceProvider ?? new DefaultServiceProvider();
        _identityProvider = identityProvider ?? new BaseIdentityProvider();
        _correlationIdAccessor = correlationIdAccessor ?? new CorrelationIdAccessor();
        _namespaceResolver = namespaceResolver ?? new DefaultEventStoreNamespaceResolver();
        _loggerFactory = loggerFactory ?? new LoggerFactory();
        _namingPolicy = namingPolicy ?? new DefaultNamingPolicy();

        var result = InitializeInternal();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;
        _artifactActivator = result.ArtifactActivator;
        _eventTypeMigrators = new EventTypeMigrators(_artifactsProvider, _serviceProvider);
        _connection = connection;
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <inheritdoc/>
    public ChronicleOptions Options { get; }

    /// <inheritdoc/>
    public ICausationManager CausationManager { get; }

    /// <summary>
    /// Gets the cancellation token used for the owned connection lifetime.
    /// </summary>
    internal CancellationToken OwnedConnectionCancellationToken => _ownedConnectionCancellation?.Token ?? CancellationToken.None;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        foreach (var eventStore in CreatedEventStores())
        {
            eventStore.ReadModelReactors.Dispose();
        }

        try
        {
            _ownedConnectionCancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        _ownedConnectionCancellation?.Dispose();
        _connection.Dispose();
    }

    /// <inheritdoc/>
    public async Task<IEventStore> GetEventStore(
        EventStoreName name,
        EventStoreNamespaceName? @namespace = null)
    {
        @namespace ??= _namespaceResolver.Resolve();
        var key = new EventStoreKey(name, @namespace);

        var lazyEventStore = _eventStores.GetOrAdd(
            key,
            static (eventStoreKey, client) => new Lazy<Task<IEventStore>>(
                () => client.CreateEventStore(eventStoreKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        try
        {
            return await lazyEventStore.Value;
        }
        catch
        {
            // A faulted task is memoized as readily as a completed one, so leaving it in place would make a single
            // failure to reach the kernel permanent for this event store: every later call replays the same
            // exception for the lifetime of the client. Removing only this instance leaves a concurrent call that
            // already succeeded alone.
            _eventStores.TryRemove(new KeyValuePair<EventStoreKey, Lazy<Task<IEventStore>>>(key, lazyEventStore));
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default)
    {
        var eventStores = await _servicesAccessor.Services.EventStores.GetEventStores();
        return eventStores.Select(_ => (EventStoreName)_).ToArray();
    }

    /// <inheritdoc/>
    public void EvictEventStores()
    {
        foreach (var eventStore in CreatedEventStores())
        {
            // Drop the RegisterAll handler the EventStore constructor wired up so the shared
            // connection's OnConnected event no longer fans out into this evicted instance.
            // Without this, the next reconnect would still run RegisterAll for every event
            // store created in the lifetime of the client, including those the caller no
            // longer cares about.
            eventStore.Connection.Lifecycle.OnConnected -= eventStore.RegisterAll;
        }
        _eventStores.Clear();
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
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver, _namingPolicy);
        var concurrencyScopeStrategies = new ConcurrencyScopeStrategies(Options.ConcurrencyOptions, _serviceProvider);
        var artifactActivator = new ClientArtifactsActivator(_serviceProvider, _loggerFactory);

        InitializeJsonSerializationOptions();

        return (causationManager, jsonSchemaGenerator, concurrencyScopeStrategies, artifactActivator);
    }

    ITokenProvider CreateTokenProvider(ChronicleOptions options, bool skipTlsValidation)
    {
        if (options.ConnectionString.AuthenticationMode == AuthenticationMode.ClientCredentials)
        {
            var username = options.ConnectionString.Username;
            var password = options.ConnectionString.Password;
            if (string.IsNullOrEmpty(username) &&
                string.IsNullOrEmpty(password))
            {
                username = ChronicleConnectionString.DevelopmentClient;
                password = ChronicleConnectionString.DevelopmentClientSecret;
            }

            return new OAuthTokenProvider(
                () => _connection is ChronicleConnection connection
                    ? connection.CurrentServerAddress
                    : options.ConnectionString.ServerAddress,
                username!,
                password!,
                skipTlsValidation,
                _loggerFactory.CreateLogger<OAuthTokenProvider>());
        }

        return new NoOpTokenProvider();
    }

    void InitializeJsonSerializationOptions()
    {
        Options.JsonSerializerOptions = new JsonSerializerOptions(Options.JsonSerializerOptions)
        {
            PropertyNamingPolicy = _namingPolicy.JsonPropertyNamingPolicy,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        Options.JsonSerializerOptions.Converters.Add(new ComplexKeyDictionaryJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new EventSourceIdJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new ConceptAsJsonConverterFactory());
        Options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new TypeJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new UriJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new PointJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new LineStringJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new PolygonJsonConverter());
        Options.JsonSerializerOptions.Converters.Add(new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());
        Options.JsonSerializerOptions.WithDeclaredCollectionsNeverNull();
    }

    async Task<IEventStore> CreateEventStore(EventStoreKey key)
    {
        var reactorSideEffectHandlers = new ReactorSideEffectHandlers(
            new EventStoreReactorSideEffectHandlerInstances(_serviceProvider));

        var eventStore = new EventStore(
            key.Name,
            key.Namespace,
            _connection,
            _artifactsProvider,
            _eventTypeMigrators,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            CausationManager,
            _identityProvider,
            _jsonSchemaGenerator,
            _namingPolicy,
            _serviceProvider,
            reactorSideEffectHandlers,
            _artifactActivator,
            Options.AutoDiscoverAndRegister,
            Options.JsonSerializerOptions,
            Options.EnableEventTypeGenerationValidation,
            Microsoft.Extensions.Options.Options.Create(Options),
            _loggerFactory);

        if (Options.AutoDiscoverAndRegister)
        {
            await eventStore.DiscoverAll();
        }

        await _connection.Connect();
        return eventStore;
    }

    /// <summary>
    /// The event stores that have actually been created, for the callers that need the instances rather than the
    /// promise of them.
    /// </summary>
    /// <returns>Every event store whose construction has completed successfully.</returns>
    /// <remarks>
    /// A cached entry is a promise of an event store rather than one: it may still be under construction, and it may
    /// have faulted. Reading <c>.Value.Result</c> on either would block or rethrow, in a disposal path that has to
    /// finish. So only the ones that completed are handed back - the rest have nothing to dispose or detach anyway.
    /// </remarks>
    IEnumerable<IEventStore> CreatedEventStores() =>
        _eventStores.Values
            .Where(_ => _.IsValueCreated && _.Value.IsCompletedSuccessfully)
            .Select(_ => _.Value.Result);

    record EventStoreKey(EventStoreName Name, EventStoreNamespaceName Namespace);
}
