using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Concepts;
using Cratis.Execution;
using Cratis.Serialization;
using Cratis.Traces;
using Cratis.Types;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="T:Cratis.Chronicle.IEventStore" />.
/// </summary>
public class EventStore : IEventStore
{
	private readonly EventStoreName _eventStoreName;

	private readonly IChronicleServicesAccessor _servicesAccessor;

	private readonly ICorrelationIdAccessor _correlationIdAccessor;

	private readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;

	private readonly ICausationManager _causationManager;

	private readonly IIdentityProvider _identityProvider;

	private readonly JsonSerializerOptions _jsonSerializerOptions;

	private readonly IClientArtifactsProvider _clientArtifactsProvider;

	private readonly IReactorSideEffectHandlers _reactorSideEffectHandlers;

	private readonly ILogger<EventStore> _logger;

	private readonly IActivitySource<EventSequence> _activitySource;

	private readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new ConcurrentDictionary<EventSequenceId, IEventSequence>();

	private readonly Cratis.Chronicle.Projections.Projections _projections;

	private readonly RegistrationRetryOptions _registrationRetry;

	private readonly RegistrationBackoff _registrationBackoff;

	private readonly DiscoveryReportBuilder? _discoveryReportBuilder;

	private readonly ArtifactIdentityDriftDetector? _identityDriftDetector;

	private readonly ArtifactIdentityDriftPolicy _identityDriftPolicy;

	private readonly bool _autoDiscoverAndRegister;

	private readonly StoreClientArtifactsProvider? _storeArtifacts;

	private SingleFlightRegistration? _registerAllFlight;

	private AsyncLocal<bool>? _backgroundRegistrationRetryContext;

	private int _backgroundRegistrationRetryActive;

	private Task? _backgroundRegistrationRetryTask;

	private CancellationTokenSource? _backgroundRegistrationRetryCancellation;

	private object? _backgroundRegistrationRetrySyncObject;

	private bool _isRegistrationDetached;

	private StorePreparationGate? _preparation;

	private volatile RegistrationOutcome _registration = RegistrationOutcome.NotRun;

	private long _identityDriftEpoch;

	private object? _registrationSync;

	private Task? _registrationTask;

	private long _disconnectEpoch;

	private long _publishedDisconnectEpoch;

	/// <inheritdoc />
	public EventStoreName Name { get; }

	/// <inheritdoc />
	public EventStoreNamespaceName Namespace { get; }

	/// <inheritdoc />
	public IChronicleConnection Connection { get; }

	/// <inheritdoc />
	public IUnitOfWorkManager UnitOfWorkManager { get; }

	/// <inheritdoc />
	public Cratis.Chronicle.Events.IEventTypes EventTypes { get; }

	/// <inheritdoc />
	public IEventSources EventSources { get; }

	/// <inheritdoc />
	public IConstraints Constraints { get; }

	/// <inheritdoc />
	public IEventLog EventLog { get; }

	/// <inheritdoc />
	public IJobs Jobs { get; }

	/// <inheritdoc />
	public IReactors Reactors { get; }

	/// <inheritdoc />
	public IOccurrences Occurrences { get; }

	/// <inheritdoc />
	public IReducers Reducers { get; }

	/// <inheritdoc />
	public IProjections Projections { get; }

	/// <inheritdoc />
	public IWebhooks Webhooks { get; }

	/// <inheritdoc />
	public IExternalServices ExternalServices { get; }

	/// <inheritdoc />
	public IEventStoreSubscriptions Subscriptions { get; }

	/// <inheritdoc />
	public IFailedPartitions FailedPartitions { get; }

	/// <inheritdoc />
	public IObservers Observers { get; }

	/// <inheritdoc />
	public Cratis.Chronicle.ReadModels.IReadModels ReadModels { get; }

	/// <inheritdoc />
	public IReadModelReactors ReadModelReactors { get; }

	/// <inheritdoc />
	public IEventSeeding Seeding { get; }

	/// <inheritdoc />
	public IPatterns Patterns { get; }

	/// <inheritdoc />
	public IPIIManager PII { get; }

	/// <inheritdoc />
	public IIdentityManager Identities { get; }

	/// <inheritdoc />
	public RegistrationOutcome Registration
	{
		get
		{
			return _registration;
		}
		private set
		{
			_registration = value;
		}
	}

	/// <inheritdoc />
	public DiscoveryReport Discovery { get; private set; } = DiscoveryReport.NotRun;

	/// <summary>
	/// Gets the monitor coordinating read-model builder ownership within this store.
	/// </summary>
	internal object ReadModelOwnershipSync { get; } = new object();

	/// <summary>
	/// Gets the serializer owned by this event store.
	/// </summary>
	internal IEventSerializer EventSerializer { get; }

	/// <summary>Gets the schema generator used for decision reads.</summary>
	internal IJsonSchemaGenerator DecisionReadSchemas { get; }

	/// <summary>Gets the JSON serializer options used for decision reads.</summary>
	internal JsonSerializerOptions DecisionReadJsonOptions { get; }

	/// <summary>
	/// Gets the currently running background registration retry loop, if <see cref="M:Cratis.Chronicle.EventStore.StartBackgroundRegistrationRetry" />
	/// has started one that has not yet finished.
	/// </summary>
	internal Task? PendingBackgroundRegistrationRetry => _backgroundRegistrationRetryTask;

	/// <summary>
	/// Gets the current preparation verdict for direct event registration readiness checks.
	/// </summary>
	internal Task LatestPreparationVerdict => _preparation?.LatestVerdict ?? Task.CompletedTask;

	private StorePreparationGate Preparation => LazyInitializer.EnsureInitialized(ref _preparation, () => new StorePreparationGate(PublishLocalPreparationFailure));

	/// <summary>
	/// Gets whether this event store was retired because its namespace was removed while the client was disconnected.
	/// A retired event store does not register again, and the client creates a new one if the namespace is used again.
	/// </summary>
	internal bool IsRetired { get; private set; }

	private object BackgroundRegistrationRetrySync => LazyInitializer.EnsureInitialized(ref _backgroundRegistrationRetrySyncObject);

	private object RegistrationSync => LazyInitializer.EnsureInitialized(ref _registrationSync);

	private long DisconnectEpoch
	{
		get
		{
			lock (RegistrationSync)
			{
				return _disconnectEpoch;
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cratis.Chronicle.EventStore" /> class.
	/// </summary>
	/// <param name="eventStoreName">Name of the event store.</param>
	/// <param name="namespace">Namespace for the event store.</param>
	/// <param name="connection"><see cref="T:Cratis.Chronicle.Connections.IChronicleConnection" /> for working with the connection to Chronicle.</param>
	/// <param name="clientArtifactsProvider"><see cref="T:Cratis.Chronicle.IClientArtifactsProvider" /> for getting client artifacts.</param>
	/// <param name="eventTypeMigrators"><see cref="T:Cratis.Chronicle.Events.Migrations.IEventTypeMigrators" /> for getting event type migrators.</param>
	/// <param name="correlationIdAccessor"><see cref="T:Cratis.Execution.ICorrelationIdAccessor" /> for getting correlation.</param>
	/// <param name="concurrencyScopeStrategies"><see cref="T:Cratis.Chronicle.EventSequences.Concurrency.IConcurrencyScopeStrategies" /> for managing concurrency scopes.</param>
	/// <param name="causationManager"><see cref="T:Cratis.Chronicle.Auditing.ICausationManager" /> for getting causation.</param>
	/// <param name="identityProvider"><see cref="T:Cratis.Chronicle.Identities.IIdentityProvider" /> for resolving identity for operations.</param>
	/// <param name="schemaGenerator"><see cref="T:Cratis.Chronicle.Schemas.IJsonSchemaGenerator" /> for generating JSON schemas.</param>
	/// <param name="namingPolicy"><see cref="T:Cratis.Serialization.INamingPolicy" /> to use for converting names during serialization.</param>
	/// <param name="serviceProvider"><see cref="T:System.IServiceProvider" /> for getting instances of services.</param>
	/// <param name="reactorSideEffectHandlers"><see cref="T:Cratis.Chronicle.Reactors.SideEffects.IReactorSideEffectHandlers" /> for handling reactor side-effect return values.</param>
	/// <param name="artifactActivator"><see cref="T:Cratis.Chronicle.IClientArtifactsActivator" /> for creating artifact instances.</param>
	/// <param name="autoDiscoverAndRegister">Whether to automatically discover and register artifacts.</param>
	/// <param name="jsonSerializerOptions"><see cref="T:System.Text.Json.JsonSerializerOptions" /> for serialization.</param>
	/// <param name="enableEventTypeGenerationValidation">Whether to enable event type generation chain validation. Defaults to <see langword="false" />.</param>
	/// <param name="options">The <see cref="T:Microsoft.Extensions.Options.IOptions`1" /> for Chronicle configuration.</param>
	/// <param name="loggerFactory"><see cref="T:Microsoft.Extensions.Logging.ILoggerFactory" /> for creating loggers.</param>
	public EventStore(EventStoreName eventStoreName, EventStoreNamespaceName @namespace, IChronicleConnection connection, IClientArtifactsProvider clientArtifactsProvider, IEventTypeMigrators eventTypeMigrators, ICorrelationIdAccessor correlationIdAccessor, IConcurrencyScopeStrategies concurrencyScopeStrategies, ICausationManager causationManager, IIdentityProvider identityProvider, IJsonSchemaGenerator schemaGenerator, INamingPolicy namingPolicy, IServiceProvider serviceProvider, IReactorSideEffectHandlers reactorSideEffectHandlers, IClientArtifactsActivator artifactActivator, bool autoDiscoverAndRegister, JsonSerializerOptions jsonSerializerOptions, bool enableEventTypeGenerationValidation, IOptions<ChronicleOptions> options, ILoggerFactory loggerFactory)
	{
		//IL_060a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0614: Expected O, but got Unknown
		//IL_0626: Unknown result type (might be due to invalid IL or missing references)
		//IL_0630: Expected O, but got Unknown
		_logger = loggerFactory.CreateLogger<EventStore>();
		_registrationRetry = options.Value.RegistrationRetry;
		_registrationBackoff = new RegistrationBackoff(_registrationRetry.InitialDelay, _registrationRetry.MaximumDelay);
		_eventStoreName = eventStoreName;
		_causationManager = causationManager;
		_identityProvider = identityProvider;
		_jsonSerializerOptions = jsonSerializerOptions;
		DecisionReadSchemas = schemaGenerator;
		DecisionReadJsonOptions = jsonSerializerOptions;
		_autoDiscoverAndRegister = autoDiscoverAndRegister;
		_preparation = new StorePreparationGate(PublishLocalPreparationFailure);
		ExplicitArtifactsSnapshot explicitArtifactsSnapshot = options.Value.ExplicitArtifacts.CreateSnapshot();
		_storeArtifacts = new StoreClientArtifactsProvider(clientArtifactsProvider, explicitArtifactsSnapshot);
		clientArtifactsProvider = _storeArtifacts;
		_clientArtifactsProvider = clientArtifactsProvider;
		_reactorSideEffectHandlers = reactorSideEffectHandlers;
		Name = eventStoreName;
		Namespace = @namespace;
		Connection = connection;
		_servicesAccessor = connection as IChronicleServicesAccessor;
		_correlationIdAccessor = correlationIdAccessor;
		_concurrencyScopeStrategies = concurrencyScopeStrategies;
		_activitySource = serviceProvider.GetRequiredKeyedService<IActivitySource<EventSequence>>("Cratis.Chronicle.Client");
		ITypes val = TypeUniverse.For(serviceProvider);
		EventTypes eventTypes = new EventTypes(this, schemaGenerator, clientArtifactsProvider, eventTypeMigrators, enableEventTypeGenerationValidation, namingPolicy);
		eventTypes.UseMigrationServices(serviceProvider);
		EventTypes = eventTypes;
		EventSources = new Cratis.Chronicle.EventSources.EventSources(this, clientArtifactsProvider);
		UnitOfWorkManager = new UnitOfWorkManager(this, serviceProvider.GetKeyedService<IActivitySource<UnitOfWork>>("Cratis.Chronicle.Client"), options.Value.UnitOfWorkLifecyclePolicy, loggerFactory.CreateLogger<UnitOfWork>());
		_correlationIdAccessor = correlationIdAccessor;
		EventSerializer = new EventSerializer(clientArtifactsProvider, artifactActivator, EventTypes, jsonSerializerOptions);
		Constraints = new Constraints(this, new global::<>z__ReadOnlyArray<ICanProvideConstraints>(new ICanProvideConstraints[4]
		{
			new ConstraintsByBuilderProvider(clientArtifactsProvider, EventTypes, namingPolicy, artifactActivator, loggerFactory.CreateLogger<ConstraintsByBuilderProvider>()),
			new UniqueConstraintProvider(clientArtifactsProvider, EventTypes, namingPolicy),
			new UniqueEventTypeConstraintsProvider(clientArtifactsProvider, EventTypes),
			new ClosesStreamConstraintsProvider(clientArtifactsProvider, EventTypes, namingPolicy)
		}));
		EventLog = new EventLog(eventStoreName, @namespace, connection, EventTypes, Constraints, EventSerializer, correlationIdAccessor, concurrencyScopeStrategies, causationManager, UnitOfWorkManager, identityProvider, jsonSerializerOptions, reactorSideEffectHandlers, EventSources);
		_sequences[EventLog.Id] = EventLog;
		Jobs = new Cratis.Chronicle.Jobs.Jobs(this);
		Cratis.Chronicle.Reactors.Reactors reactors = (Cratis.Chronicle.Reactors.Reactors)(Reactors = new Cratis.Chronicle.Reactors.Reactors(this, EventTypes, clientArtifactsProvider, serviceProvider, artifactActivator, new ReactorMiddlewaresActivator(clientArtifactsProvider, artifactActivator, loggerFactory.CreateLogger<ReactorMiddlewaresActivator>()), EventSerializer, causationManager, identityProvider, serviceProvider.GetRequiredKeyedService<IActivitySource<Cratis.Chronicle.Reactors.Reactors>>("Cratis.Chronicle.Client"), reactorSideEffectHandlers, new ReactorContextValuesBuilder((IInstancesOf<IReactorContextValuesProvider>)(object)new InstancesOf<IReactorContextValuesProvider>(val, serviceProvider)), new ReactorMethodArgumentsResolver(), loggerFactory.CreateLogger<Cratis.Chronicle.Reactors.Reactors>(), loggerFactory)
		{
			GenerationDelivery = options.Value.EventGenerationDelivery,
			MaxConcurrentPartitions = options.Value.Reactors.MaxConcurrentPartitions
		});
		Cratis.Chronicle.Occurrences.Occurrences occurrences = new Cratis.Chronicle.Occurrences.Occurrences(this);
		occurrences.Configure(clientArtifactsProvider, schemaGenerator, jsonSerializerOptions, serviceProvider, artifactActivator, causationManager, identityProvider, loggerFactory.CreateLogger<Cratis.Chronicle.Occurrences.Occurrences>(), options.Value);
		Occurrences = occurrences;
		ReducerObservers reducerObservers = new ReducerObservers();
		Reducers = new Cratis.Chronicle.Reducers.Reducers(this, clientArtifactsProvider, serviceProvider, artifactActivator, new ReducerValidator(), EventTypes, namingPolicy, jsonSerializerOptions, identityProvider, reducerObservers, serviceProvider.GetRequiredKeyedService<IActivitySource<Cratis.Chronicle.Reducers.Reducers>>("Cratis.Chronicle.Client"), loggerFactory.CreateLogger<Cratis.Chronicle.Reducers.Reducers>())
		{
			GenerationDelivery = options.Value.EventGenerationDelivery,
			MaxConcurrentPartitions = options.Value.Reducers.MaxConcurrentPartitions
		};
		Cratis.Chronicle.Projections.Projections projections = (Cratis.Chronicle.Projections.Projections)(Projections = (_projections = new Cratis.Chronicle.Projections.Projections(this, EventTypes, clientArtifactsProvider, namingPolicy, artifactActivator, jsonSerializerOptions, loggerFactory.CreateLogger<Cratis.Chronicle.Projections.Projections>())
		{
			GenerationDelivery = options.Value.EventGenerationDelivery,
			SoftwareVersion = options.Value.SoftwareVersion
		}));
		_discoveryReportBuilder = new DiscoveryReportBuilder(EventTypes, (Constraints)Constraints, reactors, Reducers, projections, namingPolicy);
		_identityDriftPolicy = options.Value.ArtifactIdentityDrift;
		_identityDriftDetector = ((_identityDriftPolicy == ArtifactIdentityDriftPolicy.Ignore) ? null : new ArtifactIdentityDriftDetector(_servicesAccessor, Name, Namespace, EventTypes, _logger));
		explicitArtifactsSnapshot.ApplyTo(projections);
		explicitArtifactsSnapshot.ApplyTo(reactors);
		Webhooks = new Cratis.Chronicle.Webhooks.Webhooks(EventTypes, this, loggerFactory.CreateLogger<Cratis.Chronicle.Webhooks.Webhooks>());
		ExternalServices = new Cratis.Chronicle.ExternalServices.ExternalServices(this, loggerFactory.CreateLogger<Cratis.Chronicle.ExternalServices.ExternalServices>());
		Subscriptions = new Cratis.Chronicle.EventStoreSubscriptions.EventStoreSubscriptions(EventTypes, this, loggerFactory.CreateLogger<Cratis.Chronicle.EventStoreSubscriptions.EventStoreSubscriptions>());
		FailedPartitions = new FailedPartitions(this);
		Observers = new Observers(this);
		ReadModelWatcherManager readModelWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, jsonSerializerOptions));
		MaterializedReadModels materializedReadModels = new MaterializedReadModels(this, projections, Reducers, schemaGenerator, _servicesAccessor, jsonSerializerOptions, loggerFactory.CreateLogger<MaterializedReadModels>());
		ReadModels = new Cratis.Chronicle.ReadModels.ReadModels(this, namingPolicy, projections, Reducers, EventTypes, schemaGenerator, options, jsonSerializerOptions, readModelWatcherManager, reducerObservers, materializedReadModels, loggerFactory.CreateLogger<Cratis.Chronicle.ReadModels.ReadModels>());
		ReadModelReactorInvoker invoker = new ReadModelReactorInvoker(reactorSideEffectHandlers, new ReactorContextValuesBuilder((IInstancesOf<IReactorContextValuesProvider>)(object)new InstancesOf<IReactorContextValuesProvider>(val, serviceProvider)), loggerFactory.CreateLogger<ReadModelReactorInvoker>());
		ReadModelReactors = new ReadModelReactors(this, clientArtifactsProvider, artifactActivator, serviceProvider, invoker, jsonSerializerOptions, loggerFactory.CreateLogger<ReadModelReactors>());
		Seeding = new EventSeeding(eventStoreName, connection, EventTypes, EventSerializer, clientArtifactsProvider, serviceProvider, artifactActivator, loggerFactory.CreateLogger<EventSeeding>(), EventSources);
		((EventSeeding)Seeding).UseGlobalSetOwner(options?.Value.ProgramIdentifier ?? "[N/A]");
		Patterns = new Cratis.Chronicle.Patterns.Patterns(this);
		PII = new PIIManager(eventStoreName, @namespace, connection);
		Identities = new IdentityManager(eventStoreName, @namespace, connection);
		if (autoDiscoverAndRegister)
		{
			Connection.Lifecycle.OnConnected += new Connected(RegisterAfterConnect);
			Connection.Lifecycle.OnDisconnected += new Disconnected(MarkRegistrationAfterDisconnect);
		}
	}

	/// <inheritdoc />
	public Task DiscoverAll()
	{
		return Preparation.Discover(DiscoverAllLocal);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Runs as a single flight per event store: a call that arrives while a run is in flight - a reconnect firing
	/// while startup registration is still on the wire, or any operation re-triggering connect after a failed run -
	/// joins that run instead of sending the kernel a second copy of the same registration. A run that follows a
	/// failed one waits an exponentially growing, jittered backoff first, so a kernel that is too slow to accept the
	/// registration sees the retry pressure back off instead of pile up. The flight is created lazily so an instance
	/// materialized without its constructor - as specification harnesses do - still registers correctly.
	/// </remarks>
	public Task RegisterAll()
	{
		Preparation.ThrowIfReentered();
		lock (RegistrationSync)
		{
			Registration = Registration with
			{
				IsRegistering = true
			};
			return _registrationTask ?? (_registrationTask = CompleteRegistration());
		}
	}

	/// <inheritdoc />
	public IEventSequence GetEventSequence(EventSequenceId id)
	{
		return _sequences.GetOrAdd(id, (EventSequenceId key, EventStore state) => new EventSequence(state._eventStoreName, state.Namespace, key, state.Connection, state.EventTypes, state.Constraints, state.EventSerializer, state._correlationIdAccessor, state._concurrencyScopeStrategies, state._causationManager, state.UnitOfWorkManager, state._identityProvider, state._jsonSerializerOptions, state._activitySource, state._reactorSideEffectHandlers, state.EventSources), this);
	}

	/// <inheritdoc />
	public async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default(CancellationToken))
	{
		ReadProtectionGuard.Read("GetNamespaces");
		INamespaces namespaces = (await ChronicleServicesAccessorExtensions.GetServices(_servicesAccessor, cancellationToken)).Namespaces;
		AllNamespacesRequest request = new AllNamespacesRequest
		{
			EventStore = _eventStoreName
		};
		CallOptions val = new CallOptions((Metadata)null, (DateTime?)null, cancellationToken, (WriteOptions)null, (ContextPropagationToken)null, (CallCredentials)null);
		return (await namespaces.AllNamespaces(request, new CallContext(ref val, (CallContextFlags)0, (object)null)).EnsureSuccess()).Select((Func<NamespaceNamesResponse, EventStoreNamespaceName>)((NamespaceNamesResponse _) => _.Name)).ToArray();
	}

	/// <inheritdoc />
	public async Task RemoveNamespace(EventStoreNamespaceName @namespace, CancellationToken cancellationToken = default(CancellationToken))
	{
		INamespaces namespaces = (await ChronicleServicesAccessorExtensions.GetServices(_servicesAccessor, cancellationToken)).Namespaces;
		RemoveNamespaceRequest request = new RemoveNamespaceRequest
		{
			EventStore = _eventStoreName,
			Namespace = ((ConceptAs<string>)(object)@namespace).Value
		};
		CallOptions val = new CallOptions((Metadata)null, (DateTime?)null, cancellationToken, (WriteOptions)null, (ContextPropagationToken)null, (CallCredentials)null);
		await namespaces.RemoveNamespace(request, new CallContext(ref val, (CallContextFlags)0, (object)null)).EnsureSuccess();
	}

	private async Task RegisterAllCore()
	{
		_logger.RegisterAllArtifacts();
		bool effectsStarted;
		Exception runFailure;
		while (true)
		{
			long epoch = DisconnectEpoch;
			bool isBackgroundRetry = (_backgroundRegistrationRetryContext?.Value ?? false) && epoch == _identityDriftEpoch;
			IImmutableList<ArtifactIdentityDrift> immutableList;
			if (!isBackgroundRetry)
			{
				IImmutableList<ArtifactIdentityDrift> empty = ImmutableList<ArtifactIdentityDrift>.Empty;
				immutableList = empty;
			}
			else
			{
				immutableList = Registration.IdentityDrift;
			}
			IImmutableList<ArtifactIdentityDrift> drift = immutableList;
			effectsStarted = false;
			runFailure = null;
			try
			{
				await Preparation.Register(_autoDiscoverAndRegister ? new Func<Task>(DiscoverAllLocal) : new Func<Task>(ValidateManualPreparation), async delegate
				{
					Func<Task> registerEventTypes = EventTypes.Register;
					Func<Task> registerReadModels = ReadModels.Register;
					try
					{
						Cratis.Chronicle.Events.IEventTypes eventTypes = EventTypes;
						EventTypes nativeEventTypes = eventTypes as EventTypes;
						if (nativeEventTypes != null)
						{
							RegisterEventTypesRequest payload = await nativeEventTypes.PrepareRegistration();
							registerEventTypes = () => nativeEventTypes.RegisterPrepared(payload);
						}
						Cratis.Chronicle.ReadModels.IReadModels readModels = ReadModels;
						Cratis.Chronicle.ReadModels.ReadModels nativeReadModels = readModels as Cratis.Chronicle.ReadModels.ReadModels;
						if (nativeReadModels != null)
						{
							RegisterManyRequest payload2 = nativeReadModels.PrepareRegistration();
							registerReadModels = () => nativeReadModels.RegisterPrepared(payload2);
						}
					}
					catch (Exception failure)
					{
						Preparation.RecordPayloadFailure(failure);
						throw;
					}
					if (!isBackgroundRetry)
					{
						drift = await DetectIdentityDrift();
						_identityDriftEpoch = epoch;
					}
					ThrowIfIdentityDriftMustFail(drift);
					effectsStarted = true;
					await RegisterAllArtifactsWithRetries(registerEventTypes, registerReadModels);
				});
			}
			catch (Exception ex)
			{
				runFailure = ex;
			}
			if (PublishRegistration(epoch, drift, runFailure))
			{
				break;
			}
		}
		if (runFailure != null)
		{
			bool flag = effectsStarted;
			if (flag)
			{
				bool flag2 = ((runFailure is ArtifactIdentityDriftDetected || runFailure is ArtifactPreparationReentered) ? true : false);
				flag = !flag2;
			}
			if (flag)
			{
				StartBackgroundRegistrationRetry();
			}
			ExceptionDispatchInfo.Capture(runFailure).Throw();
		}
	}

	private async Task<IImmutableList<ArtifactIdentityDrift>> DetectIdentityDrift()
	{
		if (_identityDriftDetector != null)
		{
			DiscoveryReport discoveryReport = BuildDiscoveryReport();
			if ((object)discoveryReport != null)
			{
				return await _identityDriftDetector.Detect(discoveryReport);
			}
		}
		return ImmutableList<ArtifactIdentityDrift>.Empty;
	}

	private void ThrowIfIdentityDriftMustFail(IImmutableList<ArtifactIdentityDrift> drift)
	{
		if (_identityDriftPolicy == ArtifactIdentityDriftPolicy.Fail)
		{
			ImmutableList<ArtifactIdentityDrift> immutableList = drift.Where(delegate(ArtifactIdentityDrift _)
			{
				ArtifactKind kind = _.Kind;
				return (kind == ArtifactKind.EventType || kind == ArtifactKind.Reactor) ? true : false;
			}).ToImmutableList();
			if (immutableList.Count > 0)
			{
				throw new ArtifactIdentityDriftDetected(immutableList);
			}
		}
	}

	private DiscoveryReport? BuildDiscoveryReport()
	{
		try
		{
			return _discoveryReportBuilder?.Build();
		}
		catch (Exception exception)
		{
			_logger.DiscoveryReportFailed(exception, Name, Namespace);
			return null;
		}
	}

	/// <summary>
	/// Keeps retrying registration in the background after <see cref="M:Cratis.Chronicle.EventStore.RegisterAllArtifactsWithRetries(System.Func{System.Threading.Tasks.Task},System.Func{System.Threading.Tasks.Task})" /> exhausts
	/// its bounded attempts, until it succeeds or the connection drops.
	/// </summary>
	/// <remarks>
	/// A registration failure here happens after the transport has already reconnected successfully - the
	/// connection watchdog only re-drives a full reconnect (and therefore a fresh registration) when the keep-alive
	/// goes stale, which a healthy transport never does again on its own. Without this, some observers can stay
	/// unsubscribed indefinitely even though the client and kernel are both otherwise fine, because nothing ever
	/// asks the kernel again. Idempotent to start - a second failure while one of these loops is already running is
	/// a no-op, since <see cref="M:Cratis.Chronicle.EventStore.RegisterAll" /> is single-flighted and this loop already owns retrying it.
	/// </remarks>
	private void StartBackgroundRegistrationRetry()
	{
		if (Interlocked.CompareExchange(ref _backgroundRegistrationRetryActive, 1, 0) != 0)
		{
			return;
		}
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		lock (BackgroundRegistrationRetrySync)
		{
			_backgroundRegistrationRetryCancellation = cancellationTokenSource;
			if (_isRegistrationDetached)
			{
				cancellationTokenSource.Cancel();
			}
		}
		_logger.EnteringBackgroundRegistrationRetry();
		_backgroundRegistrationRetryTask = RetryRegistrationInBackgroundUntilSuccessOrDisconnect(cancellationTokenSource);
	}

	private async Task RetryRegistrationInBackgroundUntilSuccessOrDisconnect(CancellationTokenSource cancellation)
	{
		CancellationToken cancellationToken = cancellation.Token;
		bool disconnected = false;
		Connection.Lifecycle.OnDisconnected += new Disconnected(StopOnDisconnect);
		try
		{
			int attempt = 1;
			while (!disconnected)
			{
				TimeSpan delay = _registrationBackoff.NextDelay(attempt);
				try
				{
					await Task.Delay(delay, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				if (disconnected || cancellationToken.IsCancellationRequested)
				{
					break;
				}
				try
				{
					await RegisterAllInBackground();
					_logger.BackgroundRegistrationRetrySucceeded(attempt);
					break;
				}
				catch (Exception ex2) when (((ex2 is ArtifactIdentityDriftDetected || ex2 is ArtifactPreparationReentered) ? true : false) || Preparation.LatestVerdict.IsFaulted)
				{
					break;
				}
				catch (Exception exception)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
					_logger.RetryingRegisterAllArtifactsInBackground(exception, attempt, delay);
					attempt++;
				}
			}
		}
		finally
		{
			Connection.Lifecycle.OnDisconnected -= new Disconnected(StopOnDisconnect);
			lock (BackgroundRegistrationRetrySync)
			{
				_backgroundRegistrationRetryCancellation = null;
				cancellation.Dispose();
			}
			Interlocked.Exchange(ref _backgroundRegistrationRetryActive, 0);
		}
		Task StopOnDisconnect()
		{
			disconnected = true;
			return Task.CompletedTask;
		}
	}

	private async Task RegisterAllInBackground()
	{
		AsyncLocal<bool> context = LazyInitializer.EnsureInitialized(ref _backgroundRegistrationRetryContext);
		context.Value = true;
		try
		{
			await RegisterAll();
		}
		finally
		{
			context.Value = false;
		}
	}

	/// <summary>
	/// Register every artifact, retrying the whole registration when the kernel does not accept it.
	/// </summary>
	/// <param name="registerEventTypes">The effect operation for the prepared event payload.</param>
	/// <param name="registerReadModels">The effect operation for the prepared read-model payload.</param>
	/// <returns>The task representing the operation.</returns>
	/// <remarks>
	/// Registration runs on the way up, so its failure is the host's failure. A kernel that is busy - catching up a
	/// newly added read model over a large event log - answers late rather than wrongly, and a host that dies on that
	/// answer restarts and re-registers, adding load to the queue it was waiting on. Waiting it out here breaks that
	/// loop. Every step is idempotent, so a retry re-runs the whole registration rather than trying to resume a
	/// partial one, and the outcome is only published once the attempts are spent - a waiter never sees the failure
	/// of an attempt that a later one went on to succeed at.
	/// </remarks>
	private async Task RegisterAllArtifactsWithRetries(Func<Task> registerEventTypes, Func<Task> registerReadModels)
	{
		int attempt = 1;
		bool flag = default(bool);
		while (true)
		{
			try
			{
				await RegisterAllArtifacts(registerEventTypes, registerReadModels);
				break;
			}
			catch (Exception ex) when (((Func<bool>)delegate
			{
				// Could not convert BlockContainer to single expression
				flag = attempt < _registrationRetry.MaxAttempts;
				if (flag)
				{
					flag = (!(ex is ArtifactIdentityDriftDetected) && !(ex is ArtifactPreparationReentered)) || 1 == 0;
				}
				return flag;
			}).Invoke())
			{
				TimeSpan delay = _registrationBackoff.NextDelay(attempt);
				_logger.RetryingRegisterAllArtifacts(ex, attempt, _registrationRetry.MaxAttempts, delay);
				await Task.Delay(delay);
				attempt++;
			}
		}
	}

	private async Task RegisterAllArtifacts(Func<Task> registerEventTypes, Func<Task> registerReadModels)
	{
		Preparation.VerifyRegistrationLease();
		await _servicesAccessor.Services.EventStores.EnsureEventStore(new EnsureEventStoreRequest
		{
			Name = ((ConceptAs<string>)(object)Name).Value
		}).EnsureSuccess();
		InlineArray3<Task> buffer = default(InlineArray3<Task>);
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray3<Task>, Task>(ref buffer, 0) = registerEventTypes();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray3<Task>, Task>(ref buffer, 1) = EventSources.Register();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray3<Task>, Task>(ref buffer, 2) = registerReadModels();
		await Task.WhenAll(global::<PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<InlineArray3<Task>, Task>(in buffer, 3));
		InlineArray5<Task> buffer2 = default(InlineArray5<Task>);
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray5<Task>, Task>(ref buffer2, 0) = Constraints.Register();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray5<Task>, Task>(ref buffer2, 1) = Reactors.Register();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray5<Task>, Task>(ref buffer2, 2) = Reducers.Register();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray5<Task>, Task>(ref buffer2, 3) = Projections.Register();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray5<Task>, Task>(ref buffer2, 4) = ((Occurrences is Cratis.Chronicle.Occurrences.Occurrences occurrences) ? occurrences.Register() : Task.CompletedTask);
		await Task.WhenAll(global::<PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<InlineArray5<Task>, Task>(in buffer2, 5));
		await RegisterExternalEventStoreSubscriptionsAsync();
		ReadModelReactors.Start();
		if (!(Seeding is EventSeeding eventSeeding))
		{
			await Seeding.Register();
		}
		else
		{
			await eventSeeding.RegisterDiscovered();
		}
	}

	private async Task RegisterExternalEventStoreSubscriptionsAsync()
	{
		string currentStoreName = ((ConceptAs<string>)(object)_eventStoreName).Value;
		IEnumerable<IReactorHandler> enumerable;
		if (!(Reactors is Cratis.Chronicle.Reactors.Reactors reactors))
		{
			enumerable = _clientArtifactsProvider.Reactors.Select(Reactors.GetHandlerFor);
		}
		else
		{
			IEnumerable<IReactorHandler> handlers = reactors.Handlers;
			enumerable = handlers;
		}
		IEnumerable<IReactorHandler> inner = enumerable;
		Dictionary<string, List<EventTypeId>> dictionary = (from item in (from type in _clientArtifactsProvider.Reactors
				where !RegisteredObserverMetadata.HasExplicitSequence(type, type.GetCustomAttribute<ReactorAttribute>()?.EventSequenceId)
				join handler in inner on type equals handler.ReactorType
				select (Type: type, HandledEventTypes: handler.EventTypes)).Concat(from type in _clientArtifactsProvider.Reducers
				where !RegisteredObserverMetadata.HasExplicitSequence(type, type.GetCustomAttribute<ReducerAttribute>()?.EventSequenceId)
				join handler in Reducers.GetAllHandlers() on type equals handler.ReducerType
				select (Type: type, HandledEventTypes: handler.EventTypes)).Select<(Type, IEnumerable<EventType>), (IEnumerable<EventType>, string)>(delegate((Type Type, IEnumerable<EventType> HandledEventTypes) observer)
			{
				string sourceStore = ObserverEventTypes.GetSourceStore(observer.Type, EventTypes, _eventStoreName, observer.HandledEventTypes);
				return (HandledEventTypes: observer.HandledEventTypes, sourceStore: sourceStore);
			})
			where item.sourceStore != null && item.sourceStore != currentStoreName
			group item.HandledEventTypes by item.sourceStore).ToDictionary((IGrouping<string, IEnumerable<EventType>> g) => g.Key, (IGrouping<string, IEnumerable<EventType>> g) => ObserverEventTypes.GetIds(g.SelectMany((IEnumerable<EventType> types) => types)).ToList());
		Dictionary<string, HashSet<EventTypeId>> dictionary2 = new Dictionary<string, HashSet<EventTypeId>>();
		foreach (KeyValuePair<string, List<EventTypeId>> item in dictionary)
		{
			Dictionary<string, HashSet<EventTypeId>> dictionary3 = dictionary2;
			string key = item.Key;
			HashSet<EventTypeId> hashSet = new HashSet<EventTypeId>();
			foreach (EventTypeId item2 in item.Value)
			{
				hashSet.Add(item2);
			}
			dictionary3[key] = hashSet;
		}
		foreach (var externalEventStoreSubscription in Projections.GetExternalEventStoreSubscriptions())
		{
			if (!dictionary2.TryGetValue(externalEventStoreSubscription.EventStoreName, out var value))
			{
				value = new HashSet<EventTypeId>();
				dictionary2[externalEventStoreSubscription.EventStoreName] = value;
			}
			foreach (EventTypeId item3 in externalEventStoreSubscription.EventTypeIds)
			{
				value.Add(item3);
			}
		}
		foreach (KeyValuePair<string, HashSet<EventTypeId>> kvp in dictionary2)
		{
			await Subscriptions.Subscribe(new EventStoreSubscriptionId(kvp.Key), kvp.Key, delegate(IEventStoreSubscriptionBuilder builder)
			{
				foreach (EventTypeId item4 in kvp.Value)
				{
					builder.WithEventType(item4);
				}
			});
		}
	}

	/// <summary>
	/// Shares discovered global seed registration with the client's other namespace stores.
	/// </summary>
	/// <param name="registration">The per-client connection registry.</param>
	internal void UseGlobalSeedRegistration(GlobalSeedRegistration registration)
	{
		((EventSeeding)Seeding).UseGlobalSeedRegistration(registration);
	}

	/// <summary>
	/// Rejects registration callback reentry before joining an unfinished task.
	/// </summary>
	internal void ThrowIfArtifactPreparationReentered()
	{
		_preparation?.ThrowIfReentered();
	}

	/// <summary>
	/// Detaches a failed store's constructor-installed callback before cache eviction, and stops its background
	/// registration retry so the abandoned store is not kept reachable by that loop.
	/// </summary>
	internal void DetachAutomaticRegistration()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		StopBackgroundRegistrationRetry();
		Connection.Lifecycle.OnConnected -= new Connected(RegisterAfterConnect);
		Connection.Lifecycle.OnDisconnected -= new Disconnected(MarkRegistrationAfterDisconnect);
		if (Occurrences is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	/// <summary>
	/// Stops the background registration retry, now and for any loop started later. Used when the store is detached
	/// and when the owning client is disposed, which does not raise a disconnect that would end the loop.
	/// </summary>
	internal void StopBackgroundRegistrationRetry()
	{
		lock (BackgroundRegistrationRetrySync)
		{
			_isRegistrationDetached = true;
			_backgroundRegistrationRetryCancellation?.Cancel();
		}
	}

	private void PublishLocalPreparationFailure(Exception failure)
	{
		lock (RegistrationSync)
		{
			_publishedDisconnectEpoch = _disconnectEpoch;
			Registration = new RegistrationOutcome(HasRun: true, _projections.ArtifactRegistrations, failure)
			{
				IdentityDrift = Registration.IdentityDrift,
				IsRegistering = (_registrationTask != null)
			};
		}
	}

	private Task ValidateManualPreparation()
	{
		if (!(EventTypes is EventTypes eventTypes))
		{
			return Task.CompletedTask;
		}
		return eventTypes.ValidatePreparation();
	}

	private async Task DiscoverAllLocal()
	{
		_logger.DiscoverAllArtifacts();
		_storeArtifacts?.Prepare();
		await EventTypes.Discover();
		await EventSources.Discover();
		InlineArray6<Task> buffer = default(InlineArray6<Task>);
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 0) = Constraints.Discover();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 1) = Reactors.Discover();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 2) = Reducers.Discover();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 3) = Projections.Discover();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 4) = Seeding.Discover();
		global::<PrivateImplementationDetails>.InlineArrayElementRef<InlineArray6<Task>, Task>(ref buffer, 5) = ((Occurrences is Cratis.Chronicle.Occurrences.Occurrences occurrences) ? occurrences.Discover() : Task.CompletedTask);
		await Task.WhenAll(global::<PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<InlineArray6<Task>, Task>(in buffer, 6));
		DiscoveryReport discoveryReport = BuildDiscoveryReport();
		if ((object)discoveryReport != null)
		{
			Discovery = discoveryReport;
			_logger.LogDiscovery(discoveryReport, Name, Namespace);
		}
	}

	/// <summary>
	/// Register after the connection was made, unless this is a reconnect and the namespace was removed meanwhile.
	/// </summary>
	/// <returns>Awaitable task.</returns>
	internal async Task RegisterAfterConnect()
	{
		bool flag = Interlocked.Read(in _disconnectEpoch) > 0 && Namespace != EventStoreNamespaceName.Default;
		if (flag)
		{
			flag = await WasNamespaceRemoved();
		}
		if (flag)
		{
			IsRetired = true;
			DetachAutomaticRegistration();
		}
		else
		{
			await RegisterAll();
		}
	}

	private async Task<bool> WasNamespaceRemoved()
	{
		try
		{
			return !(await _servicesAccessor.Services.Namespaces.AllNamespaces(new AllNamespacesRequest
			{
				EventStore = _eventStoreName
			}).EnsureSuccess()).Any((NamespaceNamesResponse _) => string.Equals(_.Name, ((ConceptAs<string>)(object)Namespace).Value, StringComparison.OrdinalIgnoreCase));
		}
		catch (Exception)
		{
			return false;
		}
	}

	private Task MarkRegistrationAfterDisconnect()
	{
		lock (RegistrationSync)
		{
			_disconnectEpoch++;
			Registration = Registration with
			{
				IsRegistering = true
			};
		}
		return Task.CompletedTask;
	}

	private async Task CompleteRegistration()
	{
		await Task.Yield();
		SingleFlightRegistration flight = LazyInitializer.EnsureInitialized(ref _registerAllFlight, () => new SingleFlightRegistration(RegisterAllCore));
		Exception failure;
		while (true)
		{
			failure = null;
			try
			{
				await flight.Run();
			}
			catch (Exception ex)
			{
				failure = ex;
			}
			lock (RegistrationSync)
			{
				if (_publishedDisconnectEpoch != _disconnectEpoch && Connection.Lifecycle.IsConnected)
				{
					continue;
				}
				Registration = Registration with
				{
					IsRegistering = (_publishedDisconnectEpoch != _disconnectEpoch)
				};
				_registrationTask = null;
				break;
			}
		}
		if (failure != null)
		{
			ExceptionDispatchInfo.Capture(failure).Throw();
		}
	}

	private bool PublishRegistration(long epoch, IImmutableList<ArtifactIdentityDrift> drift, Exception? runFailure)
	{
		bool published = false;
		Preparation.PublishRegistrationOutcome(delegate(Exception? failure)
		{
			bool isFaulted = Preparation.LatestVerdict.IsFaulted;
			lock (RegistrationSync)
			{
				bool flag = epoch != _disconnectEpoch && Connection.Lifecycle.IsConnected && !isFaulted;
				if (flag)
				{
					bool flag2 = ((failure is ArtifactIdentityDriftDetected || failure is ArtifactPreparationReentered) ? true : false);
					flag = !flag2;
				}
				if (!flag)
				{
					_publishedDisconnectEpoch = epoch;
					Registration = new RegistrationOutcome(HasRun: true, _projections.ArtifactRegistrations, failure)
					{
						IdentityDrift = drift,
						IsRegistering = (epoch != _disconnectEpoch)
					};
					published = true;
				}
			}
		}, runFailure);
		return published;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
