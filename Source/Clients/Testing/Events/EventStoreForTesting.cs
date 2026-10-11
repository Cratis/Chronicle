// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
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
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Json;
using Cratis.Serialization;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventStoreSubscriptionsImpl = Cratis.Chronicle.EventStoreSubscriptions.EventStoreSubscriptions;
using ExternalServicesImpl = Cratis.Chronicle.ExternalServices.ExternalServices;
using FailedPartitionsImpl = Cratis.Chronicle.Observation.FailedPartitions;
using JobsImpl = Cratis.Chronicle.Jobs.Jobs;
using ObserversImpl = Cratis.Chronicle.Observation.Observers;
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
public class EventStoreForTesting : IEventStore, IDisposable
{
    readonly ReadModelsForTesting _readModelsForTesting;
    readonly INamingPolicy _namingPolicy;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly EventTypes _eventTypes;
    readonly EventSources.EventSources _eventSources;

    readonly Projections.Projections _projections;
    readonly Reducers.Reducers _reducers;
    readonly ICanProvideConstraints _constraintProvider;
    readonly ClientArtifactsActivator _artifactActivator;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();
    readonly Lazy<IConstraints> _constraints;
    readonly Lazy<IReactors> _reactors;
    readonly Lazy<IWebhooks> _webhooks;
    readonly Lazy<IExternalServices> _externalServices;
    readonly Lazy<IEventStoreSubscriptions> _subscriptions;
    readonly Lazy<IFailedPartitions> _failedPartitions;
    readonly Lazy<IObservers> _observers;
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
    public EventStoreForTesting(IServiceProvider? serviceProvider, IClientArtifactsProvider clientArtifactsProvider)
        : this(serviceProvider, clientArtifactsProvider, null)
    {
    }

    /// <summary>
    /// Initializes a test event store with optional Chronicle client settings.
    /// </summary>
    /// <param name="serviceProvider">Optional service provider for resolving artifacts, options, and logging.</param>
    /// <param name="clientArtifactsProvider">Artifacts to discover.</param>
    /// <param name="options">Optional settings for the unit-of-work lifecycle policy.</param>
#pragma warning disable CA2000 // Dispose objects before losing scope
    public EventStoreForTesting(IServiceProvider? serviceProvider, IClientArtifactsProvider clientArtifactsProvider, ChronicleOptions? options)
    {
        ServiceProvider = serviceProvider ?? new DefaultServiceProvider();
        _jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        ClientArtifactsProvider = clientArtifactsProvider;
        _namingPolicy = new CamelCaseNamingPolicy();
        var loggerFactory = new NullLoggerFactory();
        _artifactActivator = new ClientArtifactsActivator(ServiceProvider, loggerFactory);
        JsonSchemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(Activate<ICanProvideComplianceMetadataForType>(ClientArtifactsProvider.ComplianceForTypesProviders)),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>(Activate<ICanProvideComplianceMetadataForProperty>(ClientArtifactsProvider.ComplianceForPropertiesProviders))),
            new SecurityMetadataResolver(
                new KnownInstancesOf<ICanProvideSecurityMetadataForType>(Activate<ICanProvideSecurityMetadataForType>(ClientArtifactsProvider.SecurityForTypesProviders)),
                new KnownInstancesOf<ICanProvideSecurityMetadataForProperty>(Activate<ICanProvideSecurityMetadataForProperty>(ClientArtifactsProvider.SecurityForPropertiesProviders))),
            _namingPolicy);

        // Discovery needs a connection first; the root resolves the registry only after discovery.
        TestingStore = new TestingEventStore(Name, Namespace, () => _eventTypes!);
        Connection = TestingStore.Connection;

        var eventTypeMigrators = new EventTypeMigrators(ClientArtifactsProvider, ServiceProvider);
        EventTypeMigrators = eventTypeMigrators;

        _eventTypes = new EventTypes(this, JsonSchemaGenerator, ClientArtifactsProvider, eventTypeMigrators, enableEventTypeGenerationValidation: false, namingPolicy: _namingPolicy);
        _eventTypes.Discover().GetAwaiter().GetResult();

        // The in-memory event log validates appends against the same definitions a real Kernel would hold.
        _eventSources = new EventSources.EventSources(this, ClientArtifactsProvider);
        _eventSources.Discover().GetAwaiter().GetResult();

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
            ServiceProvider,
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
        DecisionReads.RegisterTesting(this, new DecisionReadsForTesting(
            this,
            new DecisionReads(this, _projections, JsonSchemaGenerator, _jsonSerializerOptions),
            _readModelsForTesting));

        _constraintProvider = TestingEventStore.DiscoverConstraints(ClientArtifactsProvider, _eventTypes, _artifactActivator);
        TestingStore.Seed(_constraintProvider, JsonSchemaGenerator, eventTypeMigrators, _eventSources.All).GetAwaiter().GetResult();

        _constraints = new Lazy<IConstraints>(() => new Constraints(this, [_constraintProvider]));
        _reactors = new Lazy<IReactors>(() => new ReactorsImpl(
            this,
            _eventTypes,
            ClientArtifactsProvider,
            ServiceProvider,
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
        _observers = new Lazy<IObservers>(() => new ObserversImpl(this));
        _jobs = new Lazy<IJobs>(() => new JobsImpl(this));

        // Production EventStore reads ChronicleOptions (also configured by the ASP.NET Core host).
        // Worker hosts configure ChronicleClientOptions instead. IOptions<T> can resolve an
        // unconfigured default, so falling back must consider the policy, not just nullability.
        // DefaultServiceProvider cannot resolve unregistered interfaces.
        var configuredOptions = options ?? (ServiceProvider is DefaultServiceProvider ? null :
            ServiceProvider.GetService<IOptions<ChronicleOptions>>()?.Value);
        var lifecyclePolicy = configuredOptions?.UnitOfWorkLifecyclePolicy ?? UnitOfWorkLifecyclePolicy.Compatibility;
        if (options is null && lifecyclePolicy == UnitOfWorkLifecyclePolicy.Compatibility && ServiceProvider is not DefaultServiceProvider)
        {
            lifecyclePolicy = ServiceProvider.GetService<IOptions<ChronicleClientOptions>>()?.Value.UnitOfWorkLifecyclePolicy ?? lifecyclePolicy;
        }
        var logger = ServiceProvider is DefaultServiceProvider ? null : ServiceProvider.GetService<ILogger<UnitOfWork>>();
        _unitOfWorkManager = new Lazy<IUnitOfWorkManager>(() => new UnitOfWorkManager(this, null, lifecyclePolicy, logger));
        _patterns = new Lazy<IPatterns>(() => new Patterns.Patterns(this));
        _seeding = new Lazy<IEventSeeding>(() => new EventSeeding(
            Name,
            Connection,
            _eventTypes,
            EventSerializer,
            ClientArtifactsProvider,
            ServiceProvider,
            _artifactActivator,
            NullLogger<EventSeeding>.Instance,
            EventSources));
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
    public IEventSources EventSources => _eventSources;

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
    public IObservers Observers => _observers.Value;

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

    /// <summary>
    /// Gets the migrators activated with this store's service provider.
    /// </summary>
    internal IEventTypeMigrators EventTypeMigrators { get; }

    /// <summary>
    /// Gets the single storage root used by append and read-model processing.
    /// </summary>
    internal TestingEventStore TestingStore { get; }

    /// <summary>
    /// Gets the provider used to activate the store's artifacts.
    /// </summary>
    internal IServiceProvider ServiceProvider { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        TestingStore.Dispose();
        GC.SuppressFinalize(this);
    }

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
        var constraints = new InProcessConstraints(_constraintProvider);
        constraints.Discover().GetAwaiter().GetResult();
        return TestingStore.CreateSequence(id, EventSerializer, constraints, _eventSources);
    }

    T[] Activate<T>(IEnumerable<Type> artifactTypes)
        where T : class =>
        artifactTypes.Select(type =>
        {
            var activated = _artifactActivator.ActivateNonDisposable<T>(type);
            if (activated.TryGetException(out var error))
            {
                throw new ClientArtifactActivationFailed(type, error);
            }
            return activated.AsT0;
        }).ToArray();
}
