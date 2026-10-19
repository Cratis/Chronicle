// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.EventStoreSubscriptions;
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
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public partial class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IReactorSideEffectHandlers _reactorSideEffectHandlers;
    readonly ILogger<EventStore> _logger;
    readonly IActivitySource<EventSequence> _activitySource;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();
    readonly Projections.Projections _projections;
    readonly RegistrationRetryOptions _registrationRetry;
    readonly RegistrationBackoff _registrationBackoff;
    readonly DiscoveryReportBuilder? _discoveryReportBuilder;
    readonly ArtifactIdentityDriftDetector? _identityDriftDetector;
    readonly ArtifactIdentityDriftPolicy _identityDriftPolicy;
    readonly bool _autoDiscoverAndRegister;
    readonly StoreClientArtifactsProvider? _storeArtifacts;
    SingleFlightRegistration? _registerAllFlight;
    AsyncLocal<bool>? _backgroundRegistrationRetryContext;
    int _backgroundRegistrationRetryActive;
    Task? _backgroundRegistrationRetryTask;
    CancellationTokenSource? _backgroundRegistrationRetryCancellation;
    volatile bool _isRegistrationDetached;
    StorePreparationGate? _preparation;
    volatile RegistrationOutcome _registration = RegistrationOutcome.NotRun;
    long _identityDriftEpoch;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store.</param>
    /// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Chronicle.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="eventTypeMigrators"><see cref="IEventTypeMigrators"/> for getting event type migrators.</param>
    /// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting correlation.</param>
    /// <param name="concurrencyScopeStrategies"><see cref="IConcurrencyScopeStrategies"/> for managing concurrency scopes.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="reactorSideEffectHandlers"><see cref="IReactorSideEffectHandlers"/> for handling reactor side-effect return values.</param>
    /// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
    /// <param name="autoDiscoverAndRegister">Whether to automatically discover and register artifacts.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="enableEventTypeGenerationValidation">Whether to enable event type generation chain validation. Defaults to <see langword="false"/>.</param>
    /// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> for Chronicle configuration.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStore(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        IChronicleConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        IEventTypeMigrators eventTypeMigrators,
        ICorrelationIdAccessor correlationIdAccessor,
        IConcurrencyScopeStrategies concurrencyScopeStrategies,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IJsonSchemaGenerator schemaGenerator,
        INamingPolicy namingPolicy,
        IServiceProvider serviceProvider,
        IReactorSideEffectHandlers reactorSideEffectHandlers,
        IClientArtifactsActivator artifactActivator,
        bool autoDiscoverAndRegister,
        JsonSerializerOptions jsonSerializerOptions,
        bool enableEventTypeGenerationValidation,
        IOptions<ChronicleOptions> options,
        ILoggerFactory loggerFactory)
    {
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
        _preparation = new(PublishLocalPreparationFailure);
        var explicitSnapshot = options.Value.ExplicitArtifacts.CreateSnapshot();
        _storeArtifacts = new(clientArtifactsProvider, explicitSnapshot);
        clientArtifactsProvider = _storeArtifacts;
        _clientArtifactsProvider = clientArtifactsProvider;
        _reactorSideEffectHandlers = reactorSideEffectHandlers;
        Name = eventStoreName;
        Namespace = @namespace;
        Connection = connection;
        _servicesAccessor = (connection as IChronicleServicesAccessor)!;
        _correlationIdAccessor = correlationIdAccessor;
        _concurrencyScopeStrategies = concurrencyScopeStrategies;
        _activitySource = serviceProvider.GetRequiredKeyedService<IActivitySource<EventSequence>>(ClientActivity.SourceName);
        var types = TypeUniverse.For(serviceProvider);
        var eventTypes = new EventTypes(this, schemaGenerator, clientArtifactsProvider, eventTypeMigrators, enableEventTypeGenerationValidation, namingPolicy);
        eventTypes.UseMigrationServices(serviceProvider);
        EventTypes = eventTypes;
        EventSources = new EventSources.EventSources(this, clientArtifactsProvider);
        UnitOfWorkManager = new UnitOfWorkManager(
            this,
            serviceProvider.GetKeyedService<IActivitySource<UnitOfWork>>(ClientActivity.SourceName),
            options.Value.UnitOfWorkLifecyclePolicy,
            loggerFactory.CreateLogger<UnitOfWork>());
        _correlationIdAccessor = correlationIdAccessor;

        EventSerializer = new EventSerializer(
            clientArtifactsProvider,
            artifactActivator,
            EventTypes,
            jsonSerializerOptions);

        Constraints = new Constraints(
            this,
            [
                new ConstraintsByBuilderProvider(clientArtifactsProvider, EventTypes, namingPolicy, artifactActivator, loggerFactory.CreateLogger<ConstraintsByBuilderProvider>()),
                new UniqueConstraintProvider(clientArtifactsProvider, EventTypes, namingPolicy),
                new UniqueEventTypeConstraintsProvider(clientArtifactsProvider, EventTypes),
                new ClosesStreamConstraintsProvider(clientArtifactsProvider, EventTypes, namingPolicy)
            ]);

        EventLog = new EventLog(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            Constraints,
            EventSerializer,
            correlationIdAccessor,
            concurrencyScopeStrategies,
            causationManager,
            UnitOfWorkManager,
            identityProvider,
            jsonSerializerOptions,
            reactorSideEffectHandlers,
            EventSources);
        _sequences[EventLog.Id] = EventLog;

        Jobs = new Jobs.Jobs(this);

        var reactors = new Reactors.Reactors(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            new ReactorMiddlewaresActivator(clientArtifactsProvider, artifactActivator, loggerFactory.CreateLogger<ReactorMiddlewaresActivator>()),
            EventSerializer,
            causationManager,
            identityProvider,
            serviceProvider.GetRequiredKeyedService<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName),
            reactorSideEffectHandlers,
            new ReactorContextValuesBuilder(new InstancesOf<IReactorContextValuesProvider>(types, serviceProvider)),
            new ReactorMethodArgumentsResolver(),
            loggerFactory.CreateLogger<Reactors.Reactors>(),
            loggerFactory)
        {
            GenerationDelivery = options.Value.EventGenerationDelivery,
            MaxConcurrentPartitions = options.Value.Reactors.MaxConcurrentPartitions
        };
        Reactors = reactors;
        var occurrences = new Occurrences.Occurrences(this);
        occurrences.Configure(clientArtifactsProvider, schemaGenerator, jsonSerializerOptions, serviceProvider, artifactActivator, causationManager, identityProvider, loggerFactory.CreateLogger<Occurrences.Occurrences>(), options.Value);
        Occurrences = occurrences;

        var reducerObservers = new ReducerObservers();

        Reducers = new Reducers.Reducers(
            this,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            new ReducerValidator(),
            EventTypes,
            namingPolicy,
            jsonSerializerOptions,
            identityProvider,
            reducerObservers,
            serviceProvider.GetRequiredKeyedService<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName),
            loggerFactory.CreateLogger<Reducers.Reducers>())
        {
            GenerationDelivery = options.Value.EventGenerationDelivery,
            MaxConcurrentPartitions = options.Value.Reducers.MaxConcurrentPartitions
        };

        var projections = new Projections.Projections(
            this,
            EventTypes,
            clientArtifactsProvider,
            namingPolicy,
            artifactActivator,
            jsonSerializerOptions,
            loggerFactory.CreateLogger<Projections.Projections>())
        {
            GenerationDelivery = options.Value.EventGenerationDelivery,
            SoftwareVersion = options.Value.SoftwareVersion
        };

        _projections = projections;
        Projections = projections;
        _discoveryReportBuilder = new(EventTypes, (Constraints)Constraints, reactors, Reducers, projections, namingPolicy);
        _identityDriftPolicy = options.Value.ArtifactIdentityDrift;
        _identityDriftDetector = _identityDriftPolicy == ArtifactIdentityDriftPolicy.Ignore
            ? null
            : new(_servicesAccessor, Name, Namespace, EventTypes, _logger);

        // Registrations made with the client while it was being configured belong to every event store it creates.
        // They are built by discovery, which is when the event types they refer to become known.
        explicitSnapshot.ApplyTo(projections);
        explicitSnapshot.ApplyTo(reactors);

        Webhooks = new Webhooks.Webhooks(EventTypes, this, loggerFactory.CreateLogger<Webhooks.Webhooks>());
        ExternalServices = new ExternalServices.ExternalServices(this, loggerFactory.CreateLogger<ExternalServices.ExternalServices>());
        Subscriptions = new EventStoreSubscriptions.EventStoreSubscriptions(EventTypes, this, loggerFactory.CreateLogger<EventStoreSubscriptions.EventStoreSubscriptions>());
        FailedPartitions = new FailedPartitions(this);
        Observers = new Observers(this);

        var readModelsWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, jsonSerializerOptions));
        var materializedReadModels = new MaterializedReadModels(
            this,
            projections,
            Reducers,
            schemaGenerator,
            _servicesAccessor,
            jsonSerializerOptions,
            loggerFactory.CreateLogger<MaterializedReadModels>());

        ReadModels = new ReadModels.ReadModels(
            this,
            namingPolicy,
            projections,
            Reducers,
            EventTypes,
            schemaGenerator,
            options,
            jsonSerializerOptions,
            readModelsWatcherManager,
            reducerObservers,
            materializedReadModels,
            loggerFactory.CreateLogger<ReadModels.ReadModels>());

        var readModelReactorInvoker = new ReadModels.ReadModelReactorInvoker(
            reactorSideEffectHandlers,
            new ReactorContextValuesBuilder(new InstancesOf<IReactorContextValuesProvider>(types, serviceProvider)),
            loggerFactory.CreateLogger<ReadModels.ReadModelReactorInvoker>());

        ReadModelReactors = new ReadModels.ReadModelReactors(
            this,
            clientArtifactsProvider,
            artifactActivator,
            serviceProvider,
            readModelReactorInvoker,
            jsonSerializerOptions,
            loggerFactory.CreateLogger<ReadModels.ReadModelReactors>());

        Seeding = new EventSeeding(
            eventStoreName,
            connection,
            EventTypes,
            EventSerializer,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            loggerFactory.CreateLogger<EventSeeding>(),
            EventSources);
        ((EventSeeding)Seeding).UseGlobalSetOwner(options?.Value.ProgramIdentifier ?? "[N/A]");

        Patterns = new Patterns.Patterns(this);

        PII = new Compliance.GDPR.PIIManager(eventStoreName, @namespace, connection);
        Identities = new IdentityManager(eventStoreName, @namespace, connection);

        if (autoDiscoverAndRegister)
        {
            Connection.Lifecycle.OnConnected += RegisterAfterConnect;
            Connection.Lifecycle.OnDisconnected += MarkRegistrationAfterDisconnect;
        }
    }

    /// <inheritdoc/>
    public EventStoreName Name { get; }

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace { get; }

    /// <inheritdoc/>
    public IChronicleConnection Connection { get; }

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes { get; }

    /// <inheritdoc/>
    public IEventSources EventSources { get; }

    /// <inheritdoc/>
    public IConstraints Constraints { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IJobs Jobs { get; }

    /// <inheritdoc/>
    public IReactors Reactors { get; }

    /// <inheritdoc/>
    public IOccurrences Occurrences { get; }

    /// <inheritdoc/>
    public IReducers Reducers { get; }

    /// <inheritdoc/>
    public IProjections Projections { get; }

    /// <inheritdoc/>
    public IWebhooks Webhooks { get; }

    /// <inheritdoc/>
    public IExternalServices ExternalServices { get; }

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions { get; }

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions { get; }

    /// <inheritdoc/>
    public IObservers Observers { get; }

    /// <inheritdoc/>
    public IReadModels ReadModels { get; }

    /// <inheritdoc/>
    public IReadModelReactors ReadModelReactors { get; }

    /// <inheritdoc/>
    public IEventSeeding Seeding { get; }

    /// <inheritdoc/>
    public IPatterns Patterns { get; }

    /// <inheritdoc/>
    public Compliance.GDPR.IPIIManager PII { get; }

    /// <inheritdoc/>
    public IIdentityManager Identities { get; }

    /// <inheritdoc/>
    public RegistrationOutcome Registration
    {
        get => _registration;
        private set => _registration = value;
    }

    /// <inheritdoc/>
    public DiscoveryReport Discovery { get; private set; } = DiscoveryReport.NotRun;

    /// <summary>
    /// Gets the monitor coordinating read-model builder ownership within this store.
    /// </summary>
    internal object ReadModelOwnershipSync { get; } = new();

    /// <summary>
    /// Gets the serializer owned by this event store.
    /// </summary>
    internal IEventSerializer EventSerializer { get; }

    /// <summary>Gets the schema generator used for decision reads.</summary>
    internal IJsonSchemaGenerator DecisionReadSchemas { get; }

    /// <summary>Gets the JSON serializer options used for decision reads.</summary>
    internal JsonSerializerOptions DecisionReadJsonOptions { get; }

    /// <summary>
    /// Gets the currently running background registration retry loop, if <see cref="StartBackgroundRegistrationRetry"/>
    /// has started one that has not yet finished.
    /// </summary>
    internal Task? PendingBackgroundRegistrationRetry => _backgroundRegistrationRetryTask;

    /// <inheritdoc/>
    public Task DiscoverAll() => Preparation.Discover(DiscoverAllLocal);

    /// <inheritdoc/>
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
            Registration = Registration with { IsRegistering = true };
            return _registrationTask ??= CompleteRegistration();
        }
    }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        _sequences.GetOrAdd(
            id,
            static (key, state) => new EventSequence(
                state._eventStoreName,
                state.Namespace,
                key,
                state.Connection,
                state.EventTypes,
                state.Constraints,
                state.EventSerializer,
                state._correlationIdAccessor,
                state._concurrencyScopeStrategies,
                state._causationManager,
                state.UnitOfWorkManager,
                state._identityProvider,
                state._jsonSerializerOptions,
                state._activitySource,
                state._reactorSideEffectHandlers,
                state.EventSources),
            this);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default)
    {
        ReadProtectionGuard.Read(nameof(GetNamespaces));
        var namespaces = await (await _servicesAccessor.GetServices(cancellationToken)).Namespaces.AllNamespaces(new AllNamespacesRequest { EventStore = _eventStoreName }, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
        return namespaces.Select(_ => (EventStoreNamespaceName)_.Name).ToArray();
    }

    /// <inheritdoc/>
    public async Task RemoveNamespace(EventStoreNamespaceName @namespace, CancellationToken cancellationToken = default) =>
        await (await _servicesAccessor.GetServices(cancellationToken)).Namespaces.RemoveNamespace(new RemoveNamespaceRequest { EventStore = _eventStoreName, Namespace = @namespace.Value }, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();

    async Task RegisterAllCore()
    {
        _logger.RegisterAllArtifacts();
        while (true)
        {
            var epoch = DisconnectEpoch;
            var isBackgroundRetry = _backgroundRegistrationRetryContext?.Value == true && epoch == _identityDriftEpoch;
            var drift = isBackgroundRetry ? Registration.IdentityDrift : ImmutableList<ArtifactIdentityDrift>.Empty;
            var effectsStarted = false;
            Exception? runFailure = null;

            try
            {
                await Preparation.Register(_autoDiscoverAndRegister ? DiscoverAllLocal : ValidateManualPreparation, async () =>
                {
                    Func<Task> registerEventTypes = EventTypes.Register;
                    Func<Task> registerReadModels = ReadModels.Register;
                    try
                    {
                        if (EventTypes is EventTypes nativeEventTypes)
                        {
                            var payload = await nativeEventTypes.PrepareRegistration();
                            registerEventTypes = () => nativeEventTypes.RegisterPrepared(payload);
                        }
                        if (ReadModels is ReadModels.ReadModels nativeReadModels)
                        {
                            var payload = nativeReadModels.PrepareRegistration();
                            registerReadModels = () => nativeReadModels.RegisterPrepared(payload);
                        }
                    }
                    catch (Exception exception)
                    {
                        Preparation.RecordPayloadFailure(exception);
                        throw;
                    }
                    if (!isBackgroundRetry)
                    {
                        drift = await DetectIdentityDrift();
                        _identityDriftEpoch = epoch;
                    }

                    // Background retries reuse findings only on the same connection epoch. A restarted kernel
                    // needs a fresh snapshot, including when a reconnect joined an unfinished background attempt.
                    ThrowIfIdentityDriftMustFail(drift);
                    effectsStarted = true;
                    await RegisterAllArtifactsWithRetries(registerEventTypes, registerReadModels);
                });
            }
            catch (Exception exception)
            {
                runFailure = exception;
            }

            if (!PublishRegistration(epoch, drift, runFailure))
            {
                // A reconnect joined this flight. Its kernel needs a complete registration of its own.
                continue;
            }

            if (runFailure is not null)
            {
                if (effectsStarted && runFailure is not (ArtifactIdentityDriftDetected or ArtifactPreparationReentered))
                {
                    StartBackgroundRegistrationRetry();
                }
                ExceptionDispatchInfo.Capture(runFailure).Throw();
            }

            return;
        }
    }

    async Task<IImmutableList<ArtifactIdentityDrift>> DetectIdentityDrift()
    {
        if (_identityDriftDetector is null || BuildDiscoveryReport() is not { } current)
        {
            return ImmutableList<ArtifactIdentityDrift>.Empty;
        }

        return await _identityDriftDetector.Detect(current);
    }

    void ThrowIfIdentityDriftMustFail(IImmutableList<ArtifactIdentityDrift> drift)
    {
        if (_identityDriftPolicy != ArtifactIdentityDriftPolicy.Fail)
        {
            return;
        }

        var warnings = drift.Where(_ => _.Kind is ArtifactKind.Reactor or ArtifactKind.EventType).ToImmutableList();
        if (warnings.Count > 0)
        {
            throw new ArtifactIdentityDriftDetected(warnings);
        }
    }

    DiscoveryReport? BuildDiscoveryReport()
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
    /// Keeps retrying registration in the background after <see cref="RegisterAllArtifactsWithRetries"/> exhausts
    /// its bounded attempts, until it succeeds or the connection drops.
    /// </summary>
    /// <remarks>
    /// A registration failure here happens after the transport has already reconnected successfully - the
    /// connection watchdog only re-drives a full reconnect (and therefore a fresh registration) when the keep-alive
    /// goes stale, which a healthy transport never does again on its own. Without this, some observers can stay
    /// unsubscribed indefinitely even though the client and kernel are both otherwise fine, because nothing ever
    /// asks the kernel again. Idempotent to start - a second failure while one of these loops is already running is
    /// a no-op, since <see cref="RegisterAll"/> is single-flighted and this loop already owns retrying it.
    /// </remarks>
    void StartBackgroundRegistrationRetry()
    {
        if (Interlocked.CompareExchange(ref _backgroundRegistrationRetryActive, 1, 0) != 0)
        {
            return;
        }

        // Detaching the store cancels this source, so an abandoned store does not stay reachable through the loop.
        var cancellation = new CancellationTokenSource();
        Volatile.Write(ref _backgroundRegistrationRetryCancellation, cancellation);
        if (_isRegistrationDetached)
        {
            cancellation.Cancel();
        }

        _logger.EnteringBackgroundRegistrationRetry();

        // Tracked (not discarded) so a caller that needs to observe the loop's completion - a graceful shutdown, a
        // specification - can, without this method itself depending on anything awaiting it.
        _backgroundRegistrationRetryTask = RetryRegistrationInBackgroundUntilSuccessOrDisconnect(cancellation.Token);
    }

    async Task RetryRegistrationInBackgroundUntilSuccessOrDisconnect(CancellationToken cancellationToken)
    {
        var disconnected = false;
        Task StopOnDisconnect()
        {
            disconnected = true;
            return Task.CompletedTask;
        }

        Connection.Lifecycle.OnDisconnected += StopOnDisconnect;
        try
        {
            var attempt = 1;
            while (!disconnected)
            {
                var delay = _registrationBackoff.NextDelay(attempt);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (disconnected || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await RegisterAllInBackground();
                    _logger.BackgroundRegistrationRetrySucceeded(attempt);
                    return;
                }
                catch (Exception exception) when (exception is ArtifactIdentityDriftDetected or ArtifactPreparationReentered || Preparation.LatestVerdict.IsFaulted)
                {
                    // A failed local revision or payload is retained until explicit successful rediscovery.
                    // The local revision published its own exact failure; retrying the same verdict cannot recover it.
                    return;
                }
                catch (Exception exception)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    _logger.RetryingRegisterAllArtifactsInBackground(exception, attempt, delay);
                    attempt++;
                }
            }
        }
        finally
        {
            Connection.Lifecycle.OnDisconnected -= StopOnDisconnect;
            Interlocked.Exchange(ref _backgroundRegistrationRetryActive, 0);
        }
    }

    async Task RegisterAllInBackground()
    {
        // Execution-local state keeps an independent foreground caller's diagnostics enabled, even while this
        // loop is active. It flows into a new single flight but does not change one the retry merely joins.
        var context = LazyInitializer.EnsureInitialized(ref _backgroundRegistrationRetryContext);
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
    async Task RegisterAllArtifactsWithRetries(Func<Task> registerEventTypes, Func<Task> registerReadModels)
    {
        var attempt = 1;
        while (true)
        {
            try
            {
                await RegisterAllArtifacts(registerEventTypes, registerReadModels);
                return;
            }
            catch (Exception exception) when (attempt < _registrationRetry.MaxAttempts && exception is not (ArtifactIdentityDriftDetected or ArtifactPreparationReentered))
            {
                var delay = _registrationBackoff.NextDelay(attempt);
                _logger.RetryingRegisterAllArtifacts(exception, attempt, _registrationRetry.MaxAttempts, delay);
                await Task.Delay(delay);
                attempt++;
            }
        }
    }

    async Task RegisterAllArtifacts(Func<Task> registerEventTypes, Func<Task> registerReadModels)
    {
        Preparation.VerifyRegistrationLease();

        // Ensure the event store exists before registering artifacts.
        await _servicesAccessor.Services.EventStores.EnsureEventStore(new EnsureEventStoreRequest { Name = Name.Value }).EnsureSuccess();

        // We need to register event types and read models first, as they are used by the other artifacts
        await Task.WhenAll(
            registerEventTypes(),
            EventSources.Register(),
            registerReadModels());

        // Register all observers before seeding to prevent race conditions where
        // seeded events arrive at the kernel before observers are registered
        await Task.WhenAll(
            Constraints.Register(),
            Reactors.Register(),
            Reducers.Register(),
            Projections.Register(),
            Occurrences is Occurrences.Occurrences occurrences ? occurrences.Register() : Task.CompletedTask);

        // Auto-subscribe to any external event stores referenced by observers
        await RegisterExternalEventStoreSubscriptionsAsync();

        // Start watching read models for any registered read model reactors
        ReadModelReactors.Start();

        // Seed events only after all observers are registered
        if (Seeding is EventSeeding nativeSeeding)
        {
            await nativeSeeding.RegisterDiscovered();
        }
        else
        {
            await Seeding.Register();
        }
    }

    async Task RegisterExternalEventStoreSubscriptionsAsync()
    {
        var currentStoreName = _eventStoreName.Value;
        var reactorHandlers = Reactors is Reactors.Reactors reactors
            ? reactors.Handlers
            : _clientArtifactsProvider.Reactors.Select(Reactors.GetHandlerFor);

        var observerExternalSubscriptions = _clientArtifactsProvider.Reactors
            .Where(r => !RegisteredObserverMetadata.HasExplicitSequence(r, r.GetCustomAttribute<ReactorAttribute>()?.EventSequenceId))
            .Join(
                reactorHandlers,
                type => type,
                handler => handler.ReactorType,
                (type, handler) => (Type: type, HandledEventTypes: handler.EventTypes))
            .Concat(_clientArtifactsProvider.Reducers
                .Where(r => !RegisteredObserverMetadata.HasExplicitSequence(r, r.GetCustomAttribute<ReducerAttribute>()?.EventSequenceId))
                .Join(
                    Reducers.GetAllHandlers(),
                    type => type,
                    handler => handler.ReducerType,
                    (type, handler) => (Type: type, HandledEventTypes: handler.EventTypes)))
            .Select(observer =>
            {
                var sourceStore = ObserverEventTypes.GetSourceStore(observer.Type, EventTypes, _eventStoreName, observer.HandledEventTypes);
                return (observer.HandledEventTypes, sourceStore);
            })
            .Where(item => item.sourceStore is not null && item.sourceStore != currentStoreName)
            .GroupBy(item => item.sourceStore!, item => item.HandledEventTypes)
            .ToDictionary(
                g => g.Key,
                g => ObserverEventTypes.GetIds(g.SelectMany(types => types)).ToList());

        // Merge with projection external event store references (already filtered by GetExternalEventStoreSubscriptions)
        var allByStore = new Dictionary<string, HashSet<EventTypeId>>();
        foreach (var kvp in observerExternalSubscriptions)
        {
            allByStore[kvp.Key] = [.. kvp.Value];
        }

        foreach (var subscription in Projections.GetExternalEventStoreSubscriptions())
        {
            if (!allByStore.TryGetValue(subscription.EventStoreName, out var existing))
            {
                existing = [];
                allByStore[subscription.EventStoreName] = existing;
            }

            foreach (var id in subscription.EventTypeIds)
            {
                existing.Add(id);
            }
        }

        // Register a subscription for each external event store
        foreach (var kvp in allByStore)
        {
            await Subscriptions.Subscribe(
                new EventStoreSubscriptionId(kvp.Key),
                kvp.Key,
                builder =>
                {
                    foreach (var id in kvp.Value)
                    {
                        builder.WithEventType(id);
                    }
                });
        }
    }
}
