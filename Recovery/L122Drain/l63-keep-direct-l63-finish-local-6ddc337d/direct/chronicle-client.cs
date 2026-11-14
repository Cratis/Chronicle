using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Serialization;
using Cratis.Tasks;
using Cratis.Types;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="T:Cratis.Chronicle.IChronicleClient" />.
/// </summary>
public class ChronicleClient : IChronicleClient, IDisposable
{
	private record EventStoreKey(EventStoreName Name, EventStoreNamespaceName Namespace);

	private const string DefaultSoftwareVersion = "1.0.0";

	private const string VersionMetadataKey = "softwareVersion";

	private const string CommitMetadataKey = "softwareCommit";

	private const string DotNetClientVersionMetadataKey = ".NET Client Version";

	private const string DotNetClientCommitMetadataKey = ".NET Client Commit";

	private const string ProgramIdentifierMetadataKey = "programIdentifier";

	private const string OperatingSystemMetadataKey = "os";

	private const string MachineNameMetadataKey = "machineName";

	private const string ProcessMetadataKey = "process";

	private readonly IChronicleConnection _connection;

	private readonly IChronicleServicesAccessor _servicesAccessor;

	private readonly IJsonSchemaGenerator _jsonSchemaGenerator;

	private readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;

	private readonly IClientArtifactsActivator _artifactActivator;

	private readonly IClientArtifactsProvider _artifactsProvider;

	private readonly IServiceProvider _serviceProvider;

	private readonly IIdentityProvider _identityProvider;

	private readonly ICorrelationIdAccessor _correlationIdAccessor;

	private readonly IEventStoreNamespaceResolver _namespaceResolver;

	private readonly ILoggerFactory _loggerFactory;

	private readonly IEventTypeMigrators _eventTypeMigrators;

	private readonly INamingPolicy _namingPolicy;

	private readonly CancellationTokenSource? _ownedConnectionCancellation;

	private readonly GlobalSeedRegistration _globalSeedRegistration = new GlobalSeedRegistration();

	/// <summary>
	/// The event stores handed out, keyed on the name and namespace that identify one.
	/// </summary>
	/// <remarks>
	/// Lazy&lt;Task&lt;T&gt;&gt; with ExecutionAndPublication so that concurrent callers asking for the same event
	/// store share one construction rather than each building their own. Constructing one discovers every client
	/// artifact and connects, so the plain check-then-add this replaced let two callers do all of that twice and
	/// then handed the loser a store that was no longer the cached one.
	/// </remarks>
	private readonly ConcurrentDictionary<EventStoreKey, Lazy<Task<IEventStore>>> _eventStores = new ConcurrentDictionary<EventStoreKey, Lazy<Task<IEventStore>>>();

	private int _isDisposed;

	/// <inheritdoc />
	public ChronicleOptions Options { get; }

	/// <inheritdoc />
	public ICausationManager CausationManager { get; }

	/// <summary>
	/// Gets the cancellation token used for the owned connection lifetime.
	/// </summary>
	internal CancellationToken OwnedConnectionCancellationToken => _ownedConnectionCancellation?.Token ?? CancellationToken.None;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.ChronicleClient" /> class.
	/// </summary>
	/// <remarks>
	/// This initializes the client with the development connection string
	/// (<see cref="F:Cratis.Chronicle.Connections.ChronicleConnectionString.Development" />), which includes the default development
	/// client credentials.
	/// </remarks>
	public ChronicleClient()
		: this(ChronicleConnectionString.Development)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.ChronicleClient" /> class.
	/// </summary>
	/// <param name="connectionString">Connection string to use.</param>
	public ChronicleClient(string connectionString)
		: this(new ChronicleConnectionString(connectionString))
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_000c: Expected O, but got Unknown


	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.ChronicleClient" /> class.
	/// </summary>
	/// <param name="connectionString"><see cref="T:Cratis.Chronicle.Connections.ChronicleConnectionString" /> to connect with.</param>
	public ChronicleClient(ChronicleConnectionString connectionString)
		: this(ChronicleOptions.FromConnectionString(connectionString))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.ChronicleClient" /> class.
	/// </summary>
	/// <param name="options"><see cref="T:Cratis.Chronicle.ChronicleOptions" /> to use.</param>
	/// <param name="artifactsProvider">Optional <see cref="T:Cratis.Chronicle.IClientArtifactsProvider" />. Defaults to <see cref="F:Cratis.Chronicle.DefaultClientArtifactsProvider.Default" /> if not provided.</param>
	/// <param name="serviceProvider">Optional <see cref="T:System.IServiceProvider" />. Defaults to <see cref="T:Cratis.Chronicle.DefaultServiceProvider" /> if not provided.</param>
	/// <param name="identityProvider">Optional <see cref="T:Cratis.Chronicle.Identities.IIdentityProvider" />. Defaults to <see cref="T:Cratis.Chronicle.Identities.BaseIdentityProvider" /> if not provided.</param>
	/// <param name="correlationIdAccessor">Optional <see cref="T:Cratis.Execution.ICorrelationIdAccessor" />. Defaults to <see cref="T:Cratis.Execution.CorrelationIdAccessor" /> if not provided.</param>
	/// <param name="namespaceResolver">Optional <see cref="T:Cratis.Chronicle.IEventStoreNamespaceResolver" />. Defaults to <see cref="T:Cratis.Chronicle.DefaultEventStoreNamespaceResolver" /> if not provided.</param>
	/// <param name="loggerFactory">Optional <see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" />. Defaults to a no-op factory if not provided.</param>
	/// <param name="namingPolicy">Optional <see cref="T:Cratis.Serialization.INamingPolicy" />. Defaults to <see cref="T:Cratis.Serialization.DefaultNamingPolicy" /> if not provided.</param>
	public ChronicleClient(ChronicleOptions options, IClientArtifactsProvider? artifactsProvider = null, IServiceProvider? serviceProvider = null, IIdentityProvider? identityProvider = null, ICorrelationIdAccessor? correlationIdAccessor = null, IEventStoreNamespaceResolver? namespaceResolver = null, ILoggerFactory? loggerFactory = null, INamingPolicy? namingPolicy = null)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Expected O, but got Unknown
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Expected O, but got Unknown
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected O, but got Unknown
		Options = options;
		_artifactsProvider = artifactsProvider ?? DefaultClientArtifactsProvider.Default;
		_serviceProvider = serviceProvider ?? new DefaultServiceProvider();
		_identityProvider = identityProvider ?? new BaseIdentityProvider();
		_correlationIdAccessor = (ICorrelationIdAccessor)(((object)correlationIdAccessor) ?? ((object)new CorrelationIdAccessor()));
		_namespaceResolver = namespaceResolver ?? new DefaultEventStoreNamespaceResolver();
		_loggerFactory = loggerFactory ?? new LoggerFactory();
		_namingPolicy = (INamingPolicy)(((object)namingPolicy) ?? ((object)new DefaultNamingPolicy(true)));
		(ICausationManager, IJsonSchemaGenerator, IConcurrencyScopeStrategies, IClientArtifactsActivator) tuple = InitializeInternal();
		CausationManager = tuple.Item1;
		_jsonSchemaGenerator = tuple.Item2;
		_concurrencyScopeStrategies = tuple.Item3;
		_artifactActivator = tuple.Item4;
		_eventTypeMigrators = new EventTypeMigrators(_artifactsProvider, _serviceProvider);
		ConnectionLifecycle val = new ConnectionLifecycle(_loggerFactory.CreateLogger<ConnectionLifecycle>());
		string text = options.Tls.CertificatePath ?? options.ConnectionString.CertificatePath;
		string text2 = options.Tls.CertificatePassword ?? options.ConnectionString.CertificatePassword;
		bool flag = TlsCertificateValidationPolicy.ShouldSkip(options.Tls, options.ConnectionString);
		bool flag2 = CompatibilityCheckPolicy.ShouldSkip(options, options.ConnectionString);
		ITokenProvider val2 = CreateTokenProvider(options, flag);
		_ownedConnectionCancellation = new CancellationTokenSource();
		_connection = (IChronicleConnection)new ChronicleConnection(options.ConnectionString, options.ConnectTimeout, options.MaxReceiveMessageSize, options.MaxSendMessageSize, (IConnectionLifecycle)(object)val, (ITaskFactory)new TaskFactory(), _correlationIdAccessor, _loggerFactory, _ownedConnectionCancellation.Token, _loggerFactory.CreateLogger<ChronicleConnection>(), flag, text, text2, val2, flag2, options.SkipKeepAlive, (IChronicleServerAddressResolver)null, options.LoadBalancerStrategy, (TimeProvider)null);
		_servicesAccessor = _connection as IChronicleServicesAccessor;
		_connection.Lifecycle.OnDisconnected += new Disconnected(_globalSeedRegistration.Reset);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.ChronicleClient" /> class.
	/// </summary>
	/// <param name="connection"><see cref="T:Cratis.Chronicle.Connections.IChronicleConnection" /> to use.</param>
	/// <param name="options">Optional <see cref="T:Cratis.Chronicle.ChronicleOptions" />.</param>
	/// <param name="artifactsProvider">Optional <see cref="T:Cratis.Chronicle.IClientArtifactsProvider" />. Defaults to <see cref="F:Cratis.Chronicle.DefaultClientArtifactsProvider.Default" /> if not provided.</param>
	/// <param name="serviceProvider">Optional <see cref="T:System.IServiceProvider" />. Defaults to <see cref="T:Cratis.Chronicle.DefaultServiceProvider" /> if not provided.</param>
	/// <param name="identityProvider">Optional <see cref="T:Cratis.Chronicle.Identities.IIdentityProvider" />. Defaults to <see cref="T:Cratis.Chronicle.Identities.BaseIdentityProvider" /> if not provided.</param>
	/// <param name="correlationIdAccessor">Optional <see cref="T:Cratis.Execution.ICorrelationIdAccessor" />. Defaults to <see cref="T:Cratis.Execution.CorrelationIdAccessor" /> if not provided.</param>
	/// <param name="namespaceResolver">Optional <see cref="T:Cratis.Chronicle.IEventStoreNamespaceResolver" />. Defaults to <see cref="T:Cratis.Chronicle.DefaultEventStoreNamespaceResolver" /> if not provided.</param>
	/// <param name="loggerFactory">Optional <see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" />. Defaults to a no-op factory if not provided.</param>
	/// <param name="namingPolicy">Optional <see cref="T:Cratis.Serialization.INamingPolicy" />. Defaults to <see cref="T:Cratis.Serialization.DefaultNamingPolicy" /> if not provided.</param>
	public ChronicleClient(IChronicleConnection connection, ChronicleOptions options, IClientArtifactsProvider? artifactsProvider = null, IServiceProvider? serviceProvider = null, IIdentityProvider? identityProvider = null, ICorrelationIdAccessor? correlationIdAccessor = null, IEventStoreNamespaceResolver? namespaceResolver = null, ILoggerFactory? loggerFactory = null, INamingPolicy? namingPolicy = null)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		Options = options;
		_artifactsProvider = artifactsProvider ?? DefaultClientArtifactsProvider.Default;
		_serviceProvider = serviceProvider ?? new DefaultServiceProvider();
		_identityProvider = identityProvider ?? new BaseIdentityProvider();
		_correlationIdAccessor = (ICorrelationIdAccessor)(((object)correlationIdAccessor) ?? ((object)new CorrelationIdAccessor()));
		_namespaceResolver = namespaceResolver ?? new DefaultEventStoreNamespaceResolver();
		_loggerFactory = loggerFactory ?? new LoggerFactory();
		_namingPolicy = (INamingPolicy)(((object)namingPolicy) ?? ((object)new DefaultNamingPolicy(true)));
		(ICausationManager, IJsonSchemaGenerator, IConcurrencyScopeStrategies, IClientArtifactsActivator) tuple = InitializeInternal();
		CausationManager = tuple.Item1;
		_jsonSchemaGenerator = tuple.Item2;
		_concurrencyScopeStrategies = tuple.Item3;
		_artifactActivator = tuple.Item4;
		_eventTypeMigrators = new EventTypeMigrators(_artifactsProvider, _serviceProvider);
		_connection = connection;
		_servicesAccessor = _connection as IChronicleServicesAccessor;
		_connection.Lifecycle.OnDisconnected += new Disconnected(_globalSeedRegistration.Reset);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
		{
			return;
		}
		foreach (IEventStore item in CreatedEventStores())
		{
			(item as EventStore)?.StopBackgroundRegistrationRetry();
			item.ReadModelReactors.Dispose();
			if (item.Occurrences is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
		try
		{
			_ownedConnectionCancellation?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
		_ownedConnectionCancellation?.Dispose();
		_connection.Lifecycle.OnDisconnected -= new Disconnected(_globalSeedRegistration.Reset);
		((IDisposable)_connection).Dispose();
	}

	/// <inheritdoc />
	public async Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = null)
	{
		if ((object)@namespace == null)
		{
			@namespace = _namespaceResolver.Resolve();
		}
		EventStoreKey key = new EventStoreKey(name, @namespace);
		Lazy<Task<IEventStore>> lazyEventStore = _eventStores.GetOrAdd(key, (EventStoreKey eventStoreKey, ChronicleClient client) => new Lazy<Task<IEventStore>>(() => client.CreateEventStore(eventStoreKey), LazyThreadSafetyMode.ExecutionAndPublication), this);
		try
		{
			IEventStore eventStore = await lazyEventStore.Value;
			if (eventStore is EventStore { IsRetired: not false })
			{
				_eventStores.TryRemove(new KeyValuePair<EventStoreKey, Lazy<Task<IEventStore>>>(key, lazyEventStore));
				return await GetEventStore(name, @namespace);
			}
			return eventStore;
		}
		catch
		{
			_eventStores.TryRemove(new KeyValuePair<EventStoreKey, Lazy<Task<IEventStore>>>(key, lazyEventStore));
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default(CancellationToken))
	{
		IEventStores eventStores = (await ChronicleServicesAccessorExtensions.GetServices(_servicesAccessor, cancellationToken)).EventStores;
		CallOptions val = new CallOptions((Metadata)null, (DateTime?)null, cancellationToken, (WriteOptions)null, (ContextPropagationToken)null, (CallCredentials)null);
		return (await eventStores.AllEventStores(new CallContext(ref val, (CallContextFlags)0, (object)null)).EnsureSuccess()).Select((Func<EventStoreNamesResponse, EventStoreName>)((EventStoreNamesResponse _) => _.Name)).ToArray();
	}

	/// <inheritdoc />
	public void EvictEventStores()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		foreach (IEventStore item in CreatedEventStores())
		{
			if (item is EventStore eventStore)
			{
				eventStore.DetachAutomaticRegistration();
			}
			else
			{
				item.Connection.Lifecycle.OnConnected -= new Connected(item.RegisterAll);
			}
		}
		_eventStores.Clear();
	}

	private (ICausationManager CausationManager, IJsonSchemaGenerator JsonSchemaGenerator, IConcurrencyScopeStrategies ConcurrencyScopeStrategies, IClientArtifactsActivator ArtifactActivator) InitializeInternal()
	{
		if (Options.SoftwareVersion == "1.0.0")
		{
			_loggerFactory.CreateLogger<ChronicleClient>().SoftwareVersionIsDefault(Options.SoftwareVersion);
		}
		CausationManager causationManager = new CausationManager();
		causationManager.DefineRoot(new Dictionary<string, string>
		{
			{ "softwareVersion", Options.SoftwareVersion },
			{ "softwareCommit", Options.SoftwareCommit },
			{
				".NET Client Version",
				VersionInformation.GetChronicleClientVersion()
			},
			{
				".NET Client Commit",
				VersionInformation.GetChronicleClientCommitSha()
			},
			{ "programIdentifier", Options.ProgramIdentifier },
			{
				"os",
				Environment.OSVersion.ToString()
			},
			{
				"machineName",
				Environment.MachineName
			},
			{
				"process",
				Environment.ProcessPath ?? string.Empty
			}
		});
		ITypes val = TypeUniverse.For(_serviceProvider);
		ComplianceMetadataResolver metadataResolver = new ComplianceMetadataResolver((IInstancesOf<ICanProvideComplianceMetadataForType>)(object)new InstancesOf<ICanProvideComplianceMetadataForType>(val, _serviceProvider), (IInstancesOf<ICanProvideComplianceMetadataForProperty>)(object)new InstancesOf<ICanProvideComplianceMetadataForProperty>(val, _serviceProvider));
		SecurityMetadataResolver securityMetadataResolver = new SecurityMetadataResolver((IInstancesOf<ICanProvideSecurityMetadataForType>)(object)new InstancesOf<ICanProvideSecurityMetadataForType>(val, _serviceProvider), (IInstancesOf<ICanProvideSecurityMetadataForProperty>)(object)new InstancesOf<ICanProvideSecurityMetadataForProperty>(val, _serviceProvider));
		JsonSchemaGenerator item = new JsonSchemaGenerator(metadataResolver, securityMetadataResolver, _namingPolicy);
		ConcurrencyScopeStrategies item2 = new ConcurrencyScopeStrategies(Options.ConcurrencyOptions, _serviceProvider, _loggerFactory);
		ClientArtifactsActivator item3 = new ClientArtifactsActivator(_serviceProvider, _loggerFactory);
		InitializeJsonSerializationOptions();
		return (CausationManager: causationManager, JsonSchemaGenerator: item, ConcurrencyScopeStrategies: item2, ArtifactActivator: item3);
	}

	private ITokenProvider CreateTokenProvider(ChronicleOptions options, bool skipTlsValidation)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		if ((int)options.ConnectionString.AuthenticationMode == 0)
		{
			string text = options.ConnectionString.Username;
			string text2 = options.ConnectionString.Password;
			if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2))
			{
				text = "chronicle-dev-client";
				text2 = "chronicle-dev-secret";
			}
			return (ITokenProvider)new OAuthTokenProvider((Func<ChronicleServerAddress>)delegate
			{
				IChronicleConnection connection = _connection;
				ChronicleConnection val = (ChronicleConnection)(object)((connection is ChronicleConnection) ? connection : null);
				return (val == null) ? options.ConnectionString.ServerAddress : val.CurrentServerAddress;
			}, text, text2, skipTlsValidation, _loggerFactory.CreateLogger<OAuthTokenProvider>());
		}
		return (ITokenProvider)new NoOpTokenProvider();
	}

	private void InitializeJsonSerializationOptions()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Expected O, but got Unknown
		Options.JsonSerializerOptions = new JsonSerializerOptions(Options.JsonSerializerOptions)
		{
			PropertyNamingPolicy = _namingPolicy.JsonPropertyNamingPolicy,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false
		};
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new ComplexKeyDictionaryJsonConverterFactory());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new EnumConverterFactory());
		Options.JsonSerializerOptions.Converters.Add(new EventSourceIdJsonConverterFactory());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new EnumerableConceptAsJsonConverterFactory());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new ConceptAsJsonConverterFactory());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new DateOnlyJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new TimeOnlyJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new TypeJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new UriJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new PointJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new LineStringJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new PolygonJsonConverter());
		Options.JsonSerializerOptions.Converters.Add((JsonConverter)new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());
		Options.JsonSerializerOptions.WithDeclaredCollectionsNeverNull();
	}

	private async Task<IEventStore> CreateEventStore(EventStoreKey key)
	{
		ReactorSideEffectHandlers reactorSideEffectHandlers = new ReactorSideEffectHandlers((IInstancesOf<IReactorSideEffectHandler>)(object)new EventStoreReactorSideEffectHandlerInstances(_serviceProvider, _loggerFactory.CreateLogger<EventStoreReactorSideEffectHandlerInstances>()));
		EventStore eventStore = new EventStore(key.Name, key.Namespace, _connection, _artifactsProvider, _eventTypeMigrators, _correlationIdAccessor, _concurrencyScopeStrategies, CausationManager, _identityProvider, _jsonSchemaGenerator, _namingPolicy, _serviceProvider, reactorSideEffectHandlers, _artifactActivator, Options.AutoDiscoverAndRegister, Options.JsonSerializerOptions, Options.EnableEventTypeGenerationValidation, Microsoft.Extensions.Options.Options.Create(Options), _loggerFactory);
		eventStore.UseGlobalSeedRegistration(_globalSeedRegistration);
		try
		{
			if (Options.AutoDiscoverAndRegister)
			{
				await eventStore.DiscoverAll();
			}
			await _connection.Connect();
			if (Options.AutoDiscoverAndRegister && _connection.Lifecycle.IsConnected && !eventStore.Registration.HasRun)
			{
				await eventStore.RegisterAll();
			}
			return eventStore;
		}
		catch
		{
			eventStore.ReleaseAfterFailedCreation();
			throw;
		}
	}

	/// <summary>
	/// The event stores that have actually been created, for the callers that need the instances rather than the
	/// promise of them.
	/// </summary>
	/// <returns>Every event store whose construction has completed successfully.</returns>
	/// <remarks>
	/// A cached entry is a promise of an event store rather than one: it may still be under construction, and it may
	/// have faulted. Reading <c language="csharp">.Value.Result</c> on either would block or rethrow, in a disposal path that has to
	/// finish. So only the ones that completed are handed back - the rest have nothing to dispose or detach anyway.
	/// </remarks>
	private IEnumerable<IEventStore> CreatedEventStores()
	{
		return from _ in _eventStores.Values
			where _.IsValueCreated && _.Value.IsCompletedSuccessfully
			select _.Value.Result;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
