// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Collections.Concurrent;
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
    readonly InProcessReadModelsService _readModelsService;
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
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreForTesting"/> class.
    /// </summary>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public EventStoreForTesting()
    {
        _jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        _clientArtifactsProvider = DefaultClientArtifactsProvider.Default;
        _namingPolicy = new CamelCaseNamingPolicy();
        _jsonSchemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            _namingPolicy);

        _readModelsService = new InProcessReadModelsService(_jsonSerializerOptions);
        Connection = new ChronicleConnectionForTesting(_readModelsService);

        var serviceProvider = new DefaultServiceProvider();
        var loggerFactory = new NullLoggerFactory();
        var artifactActivator = new ClientArtifactsActivator(serviceProvider, loggerFactory);
        var eventTypeMigrators = new EventTypeMigrators(_clientArtifactsProvider, serviceProvider);

        _eventTypes = new Events.EventTypes(this, _jsonSchemaGenerator, _clientArtifactsProvider, eventTypeMigrators);
        _eventTypes.Discover().GetAwaiter().GetResult();

        _eventSerializer = new EventSerializer(_clientArtifactsProvider, artifactActivator, _eventTypes, _jsonSerializerOptions);

        var reducerObservers = new ReducerObservers();

        _projections = new Projections.Projections(
            this,
            _eventTypes,
            _clientArtifactsProvider,
            _namingPolicy,
            artifactActivator,
            _jsonSerializerOptions,
            NullLogger<Projections.Projections>.Instance);
        _projections.Discover().GetAwaiter().GetResult();

        _reducers = new Reducers.Reducers(
            this,
            _clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            new ReducerValidator(),
            _eventTypes,
            _namingPolicy,
            _jsonSerializerOptions,
            new BaseIdentityProvider(),
            reducerObservers,
            NullLogger<Reducers.Reducers>.Instance);
        _reducers.Discover().GetAwaiter().GetResult();

        var readModelWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, _jsonSerializerOptions));

        _readModels = new ReadModels.ReadModels(
            this,
            _namingPolicy,
            _projections,
            _reducers,
            _eventTypes,
            _jsonSchemaGenerator,
            _jsonSerializerOptions,
            readModelWatcherManager,
            reducerObservers);

        _constraintProvider = CreateConstraintProvider(artifactActivator);
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
    public IUnitOfWorkManager UnitOfWorkManager => throw new NotSupportedException("UnitOfWorkManager is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IConstraints Constraints => throw new NotSupportedException("Constraints is not supported directly on EventStoreForTesting.");

    /// <inheritdoc/>
    public IEventLog EventLog => (IEventLog)GetEventSequence(EventSequenceId.Log);

    /// <inheritdoc/>
    public IReactors Reactors => throw new NotSupportedException("Reactors is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IReducers Reducers => _reducers;

    /// <inheritdoc/>
    public IProjections Projections => _projections;

    /// <inheritdoc/>
    public IWebhooks Webhooks => throw new NotSupportedException("Webhooks is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions => throw new NotSupportedException("Subscriptions is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => throw new NotSupportedException("FailedPartitions is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IJobs Jobs => throw new NotSupportedException("Jobs is not supported in EventStoreForTesting.");

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels;

    /// <inheritdoc/>
    public Seeding.IEventSeeding Seeding => throw new NotSupportedException("Seeding is not supported in EventStoreForTesting.");

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
        throw new NotSupportedException("GetNamespaces is not supported in EventStoreForTesting.");

    /// <summary>
    /// Registers a pre-seeded read model instance so that production code calling
    /// <see cref="IReadModels.GetInstanceById{TReadModel}"/> can retrieve it during the test.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The event source identifier to associate with the read model instance.</param>
    /// <param name="instance">The read model instance to pre-seed.</param>
    internal void RegisterReadModelInstance<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class =>
        _readModelsService.Register(eventSourceId, instance);

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
        public System.Collections.Immutable.IImmutableList<IConstraintDefinition> Provide() =>
            providers
                .SelectMany(p => p.Provide())
                .ToImmutableList();
    }
}
