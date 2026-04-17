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
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Serialization;
using Cratis.Types;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;
using EventStoreSubscriptionsImpl = Cratis.Chronicle.EventStoreSubscriptions.EventStoreSubscriptions;
using FailedPartitionsImpl = Cratis.Chronicle.Observation.FailedPartitions;
using JobsImpl = Cratis.Chronicle.Jobs.Jobs;
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
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly INamingPolicy _namingPolicy;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IProjections _projections;
    readonly IReducers _reducers;
    readonly IReadModels _readModels;
    readonly ICanProvideConstraints _constraintProvider;
    readonly IClientArtifactsActivator _artifactActivator;
    readonly IServiceProvider _serviceProvider;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();
    readonly Lazy<IConstraints> _constraints;
    readonly Lazy<IReactors> _reactors;
    readonly Lazy<IWebhooks> _webhooks;
    readonly Lazy<IEventStoreSubscriptions> _subscriptions;
    readonly Lazy<IFailedPartitions> _failedPartitions;
    readonly Lazy<IJobs> _jobs;
    readonly Lazy<IUnitOfWorkManager> _unitOfWorkManager;
    readonly Lazy<IEventSeeding> _seeding;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreForTesting"/> class.
    /// </summary>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving reactor, reducer, and seeder instances.</param>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public EventStoreForTesting(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider ?? new DefaultServiceProvider();
        _jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        _clientArtifactsProvider = DefaultClientArtifactsProvider.Default;
        _namingPolicy = new CamelCaseNamingPolicy();
        _jsonSchemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            _namingPolicy);

        var topLevelGrainFactory = new TestingGrainFactory();
        var topLevelStorage = new InMemoryStorage(new InMemoryEventSequenceStorage(KernelSequenceConcepts::EventSequenceId.Log));
        Connection = new ChronicleConnectionForTesting(topLevelGrainFactory, topLevelStorage, _jsonSerializerOptions);

        var loggerFactory = new NullLoggerFactory();
        _artifactActivator = new ClientArtifactsActivator(_serviceProvider, loggerFactory);
        var eventTypeMigrators = new EventTypeMigrators(_clientArtifactsProvider, _serviceProvider);

        _eventTypes = new EventTypes(this, _jsonSchemaGenerator, _clientArtifactsProvider, eventTypeMigrators);
        _eventTypes.Discover().GetAwaiter().GetResult();

        _eventSerializer = new EventSerializer(_clientArtifactsProvider, _artifactActivator, _eventTypes, _jsonSerializerOptions);

        var reducerObservers = new ReducerObservers();

        _projections = new Projections.Projections(
            this,
            _eventTypes,
            _clientArtifactsProvider,
            _namingPolicy,
            _artifactActivator,
            _jsonSerializerOptions,
            NullLogger<Projections.Projections>.Instance);
        _projections.Discover().GetAwaiter().GetResult();

        _reducers = new Reducers.Reducers(
            this,
            _clientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            new ReducerValidator(),
            _eventTypes,
            _namingPolicy,
            _jsonSerializerOptions,
            new BaseIdentityProvider(),
            reducerObservers,
            NullLogger<Reducers.Reducers>.Instance);
        _reducers.Discover().GetAwaiter().GetResult();

        var readModelWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, _jsonSerializerOptions));

        var realReadModels = new Chronicle.ReadModels.ReadModels(
            this,
            _namingPolicy,
            _projections,
            _reducers,
            _eventTypes,
            _jsonSchemaGenerator,
            _jsonSerializerOptions,
            readModelWatcherManager,
            reducerObservers);

        _readModelsForTesting = new ReadModelsForTesting(realReadModels);
        _readModels = _readModelsForTesting;

        _constraintProvider = CreateConstraintProvider(_artifactActivator);

        _constraints = new Lazy<IConstraints>(() => new Constraints(this, [_constraintProvider]));
        _reactors = new Lazy<IReactors>(() => new ReactorsImpl(
            this,
            _eventTypes,
            _clientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            new ReactorMiddlewaresActivator(_clientArtifactsProvider, _artifactActivator, NullLogger<ReactorMiddlewaresActivator>.Instance),
            _eventSerializer,
            new CausationManager(),
            new BaseIdentityProvider(),
            NullLogger<ReactorsImpl>.Instance,
            new NullLoggerFactory()));
        _webhooks = new Lazy<IWebhooks>(() => new WebhooksImpl(_eventTypes, this, NullLogger<WebhooksImpl>.Instance));
        _subscriptions = new Lazy<IEventStoreSubscriptions>(() => new EventStoreSubscriptionsImpl(
            _eventTypes,
            this,
            NullLogger<EventStoreSubscriptionsImpl>.Instance));
        _failedPartitions = new Lazy<IFailedPartitions>(() => new FailedPartitionsImpl(this));
        _jobs = new Lazy<IJobs>(() => new JobsImpl(this));
        _unitOfWorkManager = new Lazy<IUnitOfWorkManager>(() => new UnitOfWorkManager(this));
        _seeding = new Lazy<IEventSeeding>(() => new EventSeeding(
            Name,
            Connection,
            _eventTypes,
            _eventSerializer,
            _clientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            NullLogger<EventSeeding>.Instance));
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
    public IEventStoreSubscriptions Subscriptions => _subscriptions.Value;

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => _failedPartitions.Value;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs.Value;

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels;

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding.Value;

    /// <summary>
    /// Gets the <see cref="IJsonSchemaGenerator"/> used by this event store.
    /// </summary>
    internal IJsonSchemaGenerator JsonSchemaGenerator => _jsonSchemaGenerator;

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> used by this event store.
    /// </summary>
    internal IClientArtifactsProvider ClientArtifactsProvider => _clientArtifactsProvider;

    /// <summary>
    /// Gets the <see cref="IEventSerializer"/> used by this event store.
    /// </summary>
    internal IEventSerializer EventSerializer => _eventSerializer;

    /// <inheritdoc/>
    public Task DiscoverAll() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RegisterAll() => Task.CompletedTask;

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

        var eventSequenceStorage = new InMemoryEventSequenceStorage(kernelEventSequenceId);
        var uniqueConstraintsStorage = new InMemoryUniqueConstraintsStorage();
        var uniqueEventTypesStorage = new InMemoryUniqueEventTypesConstraintsStorage(eventSequenceStorage);
        var constraintsStorage = new InMemoryConstraintsStorage(_constraintProvider);
        var identityStorage = new InMemoryIdentityStorage();
        var eventTypesStorage = new InMemoryEventTypesStorage();

        var storage = new InMemoryStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            identityStorage,
            eventTypesStorage);

        var grain = InProcessEventSequence.Create(
            storage,
            kernelEventSequenceId,
            kernelEventStoreName,
            kernelNamespaceName).GetAwaiter().GetResult();

        var grainFactory = new InProcessGrainFactory(grain);

        var eventSequencesService = new KernelCore::Cratis.Chronicle.Services.EventSequences.EventSequences(
            grainFactory,
            storage,
            _jsonSerializerOptions);

        var constraintsService = new InProcessNoOpConstraintsService();
        var services = new InProcessServices(eventSequencesService, constraintsService);
        var connection = new InProcessChronicleConnection(services);

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
                _eventSerializer,
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
            _eventSerializer,
            new CorrelationIdAccessor(),
            new NoConcurrencyScopeStrategies(),
            new CausationManager(),
            new NoUnitOfWorkManager(),
            new BaseIdentityProvider(),
            _jsonSerializerOptions);
    }

    ICanProvideConstraints CreateConstraintProvider(IClientArtifactsActivator artifactActivator) =>
        new CompositeConstraintProvider(
            new ConstraintsByBuilderProvider(
                _clientArtifactsProvider,
                _eventTypes,
                _namingPolicy,
                artifactActivator,
                NullLogger<ConstraintsByBuilderProvider>.Instance),
            new UniqueConstraintProvider(_clientArtifactsProvider, _eventTypes, _namingPolicy),
            new UniqueEventTypeConstraintsProvider(_clientArtifactsProvider, _eventTypes));

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
