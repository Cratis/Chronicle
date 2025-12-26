// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences.Concurrency;
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
    const string ProgramIdentifierMetadataKey = "programIdentifier";
    const string OperatingSystemMetadataKey = "os";
    const string MachineNameMetadataKey = "machineName";
    const string ProcessMetadataKey = "process";

    readonly IChronicleConnection _connection;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    readonly ConcurrentDictionary<EventStoreKey, IEventStore> _eventStores = new();

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
    /// <param name="url"><see cref="ChronicleConnectionString"/> to connect with.</param>
    public ChronicleClient(ChronicleConnectionString url)
        : this(ChronicleOptions.FromConnectionString(url))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    public ChronicleClient(ChronicleOptions options)
    {
        Options = options;
        var result = Initialize();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;

        var tokenProvider = CreateTokenProvider(options);
        var connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
        _connection = new ChronicleConnection(
            options.Url,
            options.ConnectTimeout,
            options.MaxReceiveMessageSize,
            options.MaxSendMessageSize,
            connectionLifecycle,
            new Tasks.TaskFactory(),
            options.CorrelationIdAccessor,
            options.LoggerFactory,
            CancellationToken.None,
            options.LoggerFactory.CreateLogger<ChronicleConnection>(),
            options.Tls.Disable,
            options.Tls.CertificatePath,
            options.Tls.CertificatePassword,
            options.Tls.DevelopmentCertificatePort,
            tokenProvider);
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IChronicleConnection"/> to use.</param>
    /// <param name="options">Optional <see cref="ChronicleOptions"/>.</param>
    public ChronicleClient(IChronicleConnection connection, ChronicleOptions options)
    {
        Options = options;
        var result = Initialize();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _concurrencyScopeStrategies = result.ConcurrencyScopeStrategies;
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
        @namespace ??= Options.EventStoreNamespaceResolver.Resolve();
        var key = new EventStoreKey(name, @namespace);
        if (_eventStores.TryGetValue(key, out var eventStore))
        {
            return eventStore;
        }

        Options.ArtifactsProvider.Initialize();

        eventStore = new EventStore(
            name,
            @namespace,
            _connection,
            Options.ArtifactsProvider,
            Options.CorrelationIdAccessor,
            _concurrencyScopeStrategies,
            CausationManager,
            Options.IdentityProvider,
            _jsonSchemaGenerator,
            Options.NamingPolicy,
            Options.ServiceProvider,
            Options.AutoDiscoverAndRegister,
            Options.JsonSerializerOptions,
            Options.LoggerFactory);
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

    (ICausationManager CausationManager, IJsonSchemaGenerator JsonSchemaGenerator, IConcurrencyScopeStrategies ConcurrencyScopeStrategies) Initialize()
    {
        var causationManager = new CausationManager();
        causationManager.DefineRoot(new Dictionary<string, string>
        {
            { VersionMetadataKey, Options.SoftwareVersion },
            { CommitMetadataKey, Options.SoftwareCommit },
            { ProgramIdentifierMetadataKey, Options.ProgramIdentifier },
            { OperatingSystemMetadataKey, Environment.OSVersion.ToString() },
            { MachineNameMetadataKey, Environment.MachineName },
            { ProcessMetadataKey, Environment.ProcessPath ?? string.Empty }
        });

        var complianceMetadataResolver = new ComplianceMetadataResolver(
            new InstancesOf<ICanProvideComplianceMetadataForType>(Types.Types.Instance, Options.ServiceProvider),
            new InstancesOf<ICanProvideComplianceMetadataForProperty>(Types.Types.Instance, Options.ServiceProvider));
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver, Options.NamingPolicy);
        var concurrencyScopeStrategies = new ConcurrencyScopeStrategies(Options.ConcurrencyOptions, Options.ServiceProvider);

        InitializeJsonSerializationOptions();

        return (causationManager, jsonSchemaGenerator, concurrencyScopeStrategies);
    }

    ITokenProvider CreateTokenProvider(ChronicleOptions options)
    {
        if (options.Authentication.Mode == AuthenticationMode.ClientCredentials)
        {
            return new OAuthTokenProvider(
                options.Url.ServerAddress,
                options.Authentication.Username,
                options.Authentication.Password,
                options.ManagementPort,
                options.Tls.Disable,
                options.LoggerFactory.CreateLogger<OAuthTokenProvider>());
        }

        return new NoOpTokenProvider();
    }

    void InitializeJsonSerializationOptions()
    {
        Options.JsonSerializerOptions.PropertyNamingPolicy = Options.NamingPolicy.JsonPropertyNamingPolicy;
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
