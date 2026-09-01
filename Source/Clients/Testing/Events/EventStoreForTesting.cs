// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Testing.Compliance;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Serialization;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.Options;
using EventStoreSubscriptionsImpl = Cratis.Chronicle.EventStoreSubscriptions.EventStoreSubscriptions;
using ExternalServicesImpl = Cratis.Chronicle.ExternalServices.ExternalServices;
using FailedPartitionsImpl = Cratis.Chronicle.Observation.FailedPartitions;
using InMemoryClosedStreamsConstraintStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.ClosedStreamsConstraintStorage;
using InMemoryEventSequenceStorage = Cratis.Chronicle.Storage.InMemory.EventSequences.EventSequenceStorage;
using InMemoryUniqueConstraintsStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.UniqueConstraintsStorage;
using InMemoryUniqueEventTypesConstraintsStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.UniqueEventTypesConstraintsStorage;
using JobsImpl = Cratis.Chronicle.Jobs.Jobs;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;
using ReactorsImpl = Cratis.Chronicle.Reactors.Reactors;
using WebhooksImpl = Cratis.Chronicle.Webhooks.Webhooks;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/> for testing.
/// </summary>
/// <remarks>
/// Provides a fully in-process event store backed by real client implementations wired to
/// in-process contract service implementations — no live Chronicle server required.
/// </remarks>
public class EventStoreForTesting : IEventStore
{
    readonly ReadModelsForTesting _readModelsForTesting;
    readonly InProcessCompliance _compliance = new();
    readonly INamingPolicy _namingPolicy;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly EventTypes _eventTypes;
    readonly Projections.Projections _projections;
    readonly Reducers.Reducers _reducers;
    readonly ICanProvideConstraints _constraintProvider;
    readonly IClientArtifactsActivator _artifactActivator;
    readonly IServiceProvider _serviceProvider;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();
    readonly Lazy<IConstraints> _constraints;
    readonly Lazy<IReactors> _reactors;
    readonly Lazy<IWebhooks> _webhooks;
    readonly Lazy<IExternalServices> _externalServices;
    readonly Lazy<IEventStoreSubscriptions> _subscriptions;
    readonly Lazy<IFailedPartitions> _failedPartitions;
    readonly Lazy<IJobs> _jobs;
    readonly Lazy<IUnitOfWorkManager> _unitOfWorkManager;
    readonly Lazy<IEventSeeding> _seeding;
    readonly Lazy<IPatterns> _patterns;
    readonly Lazy<IPIIManager> _pii;
    readonly Lazy<IIdentityManager> _identities;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreForTesting"/> class.
    /// </summary>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reactor, reducer, and seeder instances.</param>
    public EventStoreForTesting(IServiceProvider? serviceProvider = null)
        : this(serviceProvider, DefaultClientArtifactsProvider.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreForTesting"/> class.
    /// </summary>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reactor, reducer, and seeder instances.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> to use for artifact discovery.</param>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public EventStoreForTesting(IServiceProvider? serviceProvider, IClientArtifactsProvider clientArtifactsProvider)
    {
        _serviceProvider = serviceProvider ?? new DefaultServiceProvider();
        _jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        ClientArtifactsProvider = clientArtifactsProvider;
        _namingPolicy = new CamelCaseNamingPolicy();

        var loggerFactory = new NullLoggerFactory();
        _artifactActivator = new ClientArtifactsActivator(_serviceProvider, loggerFactory);

        // The compliance metadata providers have to be the discovered ones. Resolving with none - which this
        // used to do - leaves every generated schema without compliance metadata, and the schema is the gate
        // the kernel checks before it encrypts anything, so a [PII] marker would be silently inert no matter
        // how the compliance manager itself was wired.
        JsonSchemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>([.. Activate<ICanProvideComplianceMetadataForType>(ClientArtifactsProvider.ComplianceForTypesProviders)]),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([.. Activate<ICanProvideComplianceMetadataForProperty>(ClientArtifactsProvider.ComplianceForPropertiesProviders)])),
            _namingPolicy);

        var topLevelGrainFactory = new TestingGrainFactory();
        var topLevelStorage = new InMemoryStorage(new InMemoryEventSequenceStorage(
            (KernelConceptsNs::EventStoreName)(string)Name,
            (KernelConceptsNs::EventStoreNamespaceName)(string)Namespace,
            KernelSequenceConcepts::EventSequenceId.Log));
        Connection = new ChronicleConnectionForTesting(topLevelGrainFactory, topLevelStorage, _compliance, _jsonSerializerOptions);

        var eventTypeMigrators = new EventTypeMigrators(ClientArtifactsProvider, _serviceProvider);

        _eventTypes = new EventTypes(this, JsonSchemaGenerator, ClientArtifactsProvider, eventTypeMigrators);
        _eventTypes.Discover().GetAwaiter().GetResult();

        EventSerializer = new EventSerializer(ClientArtifactsProvider, _artifactActivator, _eventTypes, _jsonSerializerOptions);

        var reducerObservers = new ReducerObservers();

        _projections = new Projections.Projections(
            this,
            _eventTypes,
            ClientArtifactsProvider,
            _namingPolicy,
            _artifactActivator,
            _jsonSerializerOptions,
            NullLogger<Projections.Projections>.Instance);
        _projections.Discover().GetAwaiter().GetResult();

        _reducers = new Reducers.Reducers(
            this,
            ClientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            new ReducerValidator(),
            _eventTypes,
            _namingPolicy,
            _jsonSerializerOptions,
            new BaseIdentityProvider(),
            reducerObservers,
            new ActivitySource<Reducers.Reducers>(),
            NullLogger<Reducers.Reducers>.Instance);
        _reducers.Discover().GetAwaiter().GetResult();

        var readModelWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, _jsonSerializerOptions));

        var materializedReadModels = new MaterializedReadModels(
            this,
            _projections,
            _reducers,
            JsonSchemaGenerator,
            (Connection as IChronicleServicesAccessor)!,
            _jsonSerializerOptions,
            NullLogger<MaterializedReadModels>.Instance);

        var realReadModels = new Chronicle.ReadModels.ReadModels(
            this,
            _namingPolicy,
            _projections,
            _reducers,
            _eventTypes,
            JsonSchemaGenerator,
            Options.Create(new ChronicleOptions()),
            _jsonSerializerOptions,
            readModelWatcherManager,
            reducerObservers,
            materializedReadModels,
            NullLogger<Chronicle.ReadModels.ReadModels>.Instance);

        _readModelsForTesting = new ReadModelsForTesting(realReadModels);
        ReadModels = _readModelsForTesting;

        _constraintProvider = CreateConstraintProvider(_artifactActivator);

        _constraints = new Lazy<IConstraints>(() => new Constraints(this, [_constraintProvider]));
        _reactors = new Lazy<IReactors>(() => new ReactorsImpl(
            this,
            _eventTypes,
            ClientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            new ReactorMiddlewaresActivator(ClientArtifactsProvider, _artifactActivator, NullLogger<ReactorMiddlewaresActivator>.Instance),
            EventSerializer,
            new CausationManager(),
            new BaseIdentityProvider(),
            new ActivitySource<ReactorsImpl>(),
            new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>(
            [
                new EventResultHandler(),
                new EventsResultHandler(),
                new EventForEventSourceIdResultHandler(),
                new EventsForEventSourceIdResultHandler(),
                new MixedSideEffectsResultHandler(),
                new EventsWithConcurrencyScopesResultHandler()
            ])),
            new ReactorContextValuesBuilder(new KnownInstancesOf<IReactorContextValuesProvider>(
            [
                new EventSourceIdValuesProvider(),
                new EventStreamIdValuesProvider(),
                new EventStreamTypeValuesProvider(),
                new EventSourceTypeValuesProvider(),
                new SubjectValuesProvider()
            ])),
            new ReactorMethodArgumentsResolver(),
            NullLogger<ReactorsImpl>.Instance,
            new NullLoggerFactory()));
        _webhooks = new Lazy<IWebhooks>(() => new WebhooksImpl(_eventTypes, this, NullLogger<WebhooksImpl>.Instance));
        _externalServices = new Lazy<IExternalServices>(() => new ExternalServicesImpl(this, NullLogger<ExternalServicesImpl>.Instance));
        _subscriptions = new Lazy<IEventStoreSubscriptions>(() => new EventStoreSubscriptionsImpl(
            _eventTypes,
            this,
            NullLogger<EventStoreSubscriptionsImpl>.Instance));
        _failedPartitions = new Lazy<IFailedPartitions>(() => new FailedPartitionsImpl(this));
        _jobs = new Lazy<IJobs>(() => new JobsImpl(this));
        _unitOfWorkManager = new Lazy<IUnitOfWorkManager>(() => new UnitOfWorkManager(this));
        _patterns = new Lazy<IPatterns>(() => new Patterns.Patterns(this));

        _seeding = new Lazy<IEventSeeding>(() => new EventSeeding(
            Name,
            Connection,
            _eventTypes,
            EventSerializer,
            ClientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            NullLogger<EventSeeding>.Instance));
        _pii = new Lazy<IPIIManager>(() => new PIIManager(Name, Namespace, Connection));
        _identities = new Lazy<IIdentityManager>(() => new IdentityManager(Name, Namespace, Connection));
    }
#pragma warning restore CA2000 // Dispose objects before losing scope

    /// <inheritdoc/>
    public EventStoreName Name => "testing";

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace => "default";

    /// <inheritdoc/>
    public IChronicleConnection Connection { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes => _eventTypes;

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager => _unitOfWorkManager.Value;

    /// <inheritdoc/>
    public IConstraints Constraints => _constraints.Value;

    /// <inheritdoc/>
    public IEventLog EventLog => (IEventLog)GetEventSequence(EventSequenceId.Log);

    /// <inheritdoc/>
    public IReactors Reactors => _reactors.Value;

    /// <inheritdoc/>
    public IReducers Reducers => _reducers;

    /// <inheritdoc/>
    public IProjections Projections => _projections;

    /// <inheritdoc/>
    public IWebhooks Webhooks => _webhooks.Value;

    /// <inheritdoc/>
    public IExternalServices ExternalServices => _externalServices.Value;

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions => _subscriptions.Value;

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => _failedPartitions.Value;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs.Value;

    /// <inheritdoc/>
    public IReadModels ReadModels { get; }

    /// <inheritdoc/>
    public IReadModelReactors ReadModelReactors { get; } = new NullReadModelReactors();

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding.Value;

    /// <inheritdoc/>
    /// <remarks>
    /// Backed by the real client implementation, so a scenario that asks what a scope usually does gets the answer
    /// the in-process services hold rather than an exception from a surface it is exercising.
    /// </remarks>
    public IPatterns Patterns => _patterns.Value;

    /// <inheritdoc/>
    public IPIIManager PII => _pii.Value;

    /// <inheritdoc/>
    public IIdentityManager Identities => _identities.Value;

    /// <inheritdoc/>
    public RegistrationOutcome Registration { get; private set; } = RegistrationOutcome.NotRun;

    /// <summary>
    /// Gets the <see cref="IJsonSchemaGenerator"/> used by this event store.
    /// </summary>
    internal IJsonSchemaGenerator JsonSchemaGenerator { get; }

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> used by this event store.
    /// </summary>
    internal IClientArtifactsProvider ClientArtifactsProvider { get; }

    /// <summary>
    /// Gets the <see cref="IEventSerializer"/> used by this event store.
    /// </summary>
    internal IEventSerializer EventSerializer { get; }

    /// <inheritdoc/>
    public Task DiscoverAll() => Task.CompletedTask;

    /// <inheritdoc/>
    /// <remarks>
    /// There is no kernel to register with - the artifacts are already wired to in-process services - so this only
    /// publishes the outcome discovery arrived at, keeping the same <see cref="RegistrationOutcome.NotRun"/>-until-run
    /// transition a live event store has.
    /// </remarks>
    public Task RegisterAll()
    {
        Registration = new RegistrationOutcome(true, _projections.ArtifactRegistrations);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        _sequences.GetOrAdd(id, CreateEventSequence);

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<EventStoreNamespaceName>());

    /// <summary>
    /// Registers a pre-seeded read model instance so that production code calling
    /// <see cref="IReadModels.GetInstanceById{TReadModel}"/> can retrieve it during the test.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The event source identifier to associate with the read model instance.</param>
    /// <param name="instance">The read model instance to pre-seed.</param>
    internal void RegisterReadModelInstance<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class =>
        _readModelsForTesting.RegisterInstance(eventSourceId, instance);

    IEventSequence CreateEventSequence(EventSequenceId id)
    {
        var kernelEventSequenceId = (KernelSequenceConcepts::EventSequenceId)(string)id;
        var kernelEventStoreName = (KernelConceptsNs::EventStoreName)(string)Name;
        var kernelNamespaceName = (KernelConceptsNs::EventStoreNamespaceName)(string)Namespace;

        var eventSequenceStorage = new InMemoryEventSequenceStorage(kernelEventStoreName, kernelNamespaceName, kernelEventSequenceId);
        var uniqueConstraintsStorage = new InMemoryUniqueConstraintsStorage();
        var uniqueEventTypesStorage = new InMemoryUniqueEventTypesConstraintsStorage(eventSequenceStorage);
        var closedStreamsStorage = new InMemoryClosedStreamsConstraintStorage();
        var constraintsStorage = new InMemoryConstraintsStorage(_constraintProvider);
        var identityStorage = new InMemoryIdentityStorage();
        var eventTypesStorage = new InMemoryEventTypesStorage(_eventTypes);

        var storage = new InMemoryStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            closedStreamsStorage,
            identityStorage,
            eventTypesStorage);

        var grain = InProcessEventSequence.Create(
            storage,
            kernelEventSequenceId,
            kernelEventStoreName,
            kernelNamespaceName,
            _compliance).GetAwaiter().GetResult();

        var grainFactory = new InProcessGrainFactory(grain);

        var eventSequencesService = new KernelCore::Cratis.Chronicle.Services.EventSequences.EventSequences(
            grainFactory,
            storage,
            _compliance.CreateEventCompliance(),
            _jsonSerializerOptions);

        var constraintsService = new InProcessNoOpConstraintsService();
        var services = new InProcessServices(eventSequencesService, constraintsService);
#pragma warning disable CA2000 // Dispose objects before losing scope — EventLog/EventSequence takes ownership
        var connection = new InProcessChronicleConnection(services);
#pragma warning restore CA2000

        var inProcessConstraints = new InProcessConstraints(_constraintProvider);
        inProcessConstraints.Discover().GetAwaiter().GetResult();

        if (id == EventSequenceId.Log)
        {
            return new EventLog(
                Name,
                Namespace,
                connection,
                _eventTypes,
                inProcessConstraints,
                EventSerializer,
                new CorrelationIdAccessor(),
                new NoConcurrencyScopeStrategies(),
                new CausationManager(),
                new NoUnitOfWorkManager(),
                new BaseIdentityProvider(),
                _jsonSerializerOptions);
        }

        return new EventSequence(
            Name,
            Namespace,
            id,
            connection,
            _eventTypes,
            inProcessConstraints,
            EventSerializer,
            new CorrelationIdAccessor(),
            new NoConcurrencyScopeStrategies(),
            new CausationManager(),
            new NoUnitOfWorkManager(),
            new BaseIdentityProvider(),
            _jsonSerializerOptions);
    }

    /// <summary>
    /// Activates the discovered artifact types of a given kind, skipping any that cannot be constructed.
    /// </summary>
    /// <typeparam name="T">The contract the artifacts implement.</typeparam>
    /// <param name="artifactTypes">The discovered artifact types.</param>
    /// <returns>The instances that could be activated.</returns>
    T[] Activate<T>(IEnumerable<Type> artifactTypes)
        where T : class =>
        artifactTypes
            .Select(_artifactActivator.ActivateNonDisposable<T>)
            .Where(activated => !activated.TryGetException(out _))
            .Select(activated => activated.AsT0)
            .ToArray();

    CompositeConstraintProvider CreateConstraintProvider(IClientArtifactsActivator artifactActivator) =>
        new(
            new ConstraintsByBuilderProvider(
                ClientArtifactsProvider,
                _eventTypes,
                _namingPolicy,
                artifactActivator,
                NullLogger<ConstraintsByBuilderProvider>.Instance),
            new UniqueConstraintProvider(ClientArtifactsProvider, _eventTypes, _namingPolicy),
            new UniqueEventTypeConstraintsProvider(ClientArtifactsProvider, _eventTypes));

    sealed class NoConcurrencyScopeStrategies : IConcurrencyScopeStrategies
    {
        /// <inheritdoc/>
        public IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence) => NoConcurrencyScopeStrategy.Instance;
    }

    sealed class NoConcurrencyScopeStrategy : IConcurrencyScopeStrategy
    {
        internal static readonly NoConcurrencyScopeStrategy Instance = new();

        /// <inheritdoc/>
        public Task<ConcurrencyScope> GetScope(
            EventSourceId eventSourceId,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            IEnumerable<EventType>? eventTypes = default) =>
            Task.FromResult(ConcurrencyScope.None);
    }

    sealed class NoUnitOfWorkManager : IUnitOfWorkManager
    {
        /// <inheritdoc/>
        public IUnitOfWork Current => throw new NoUnitOfWorkHasBeenStarted();

        /// <inheritdoc/>
        public bool HasCurrent => false;

        /// <inheritdoc/>
        public bool TryGetFor(CorrelationId correlationId, [MaybeNullWhen(false)] out IUnitOfWork unitOfWork)
        {
            unitOfWork = null;
            return false;
        }

        /// <inheritdoc/>
        public IUnitOfWork Begin(CorrelationId correlationId) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");

        /// <inheritdoc/>
        public void SetCurrent(IUnitOfWork unitOfWork) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");
    }

    sealed class CompositeConstraintProvider(params ICanProvideConstraints[] providers) : ICanProvideConstraints
    {
        /// <inheritdoc/>
        public IImmutableList<IConstraintDefinition> Provide() =>
            providers
                .SelectMany(p => p.Provide())
                .ToImmutableList();
    }
}
