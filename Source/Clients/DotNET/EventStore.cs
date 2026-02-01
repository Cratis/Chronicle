// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IEventSerializer _eventSerializer;
    readonly ILogger<EventStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store.</param>
    /// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Chronicle.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting correlation.</param>
    /// <param name="concurrencyScopeStrategies"><see cref="IConcurrencyScopeStrategies"/> for managing concurrency scopes.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="autoDiscoverAndRegister">Whether to automatically discover and register artifacts.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStore(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        IChronicleConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        ICorrelationIdAccessor correlationIdAccessor,
        IConcurrencyScopeStrategies concurrencyScopeStrategies,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IJsonSchemaGenerator schemaGenerator,
        INamingPolicy namingPolicy,
        IServiceProvider serviceProvider,
        bool autoDiscoverAndRegister,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventStore>();
        _eventStoreName = eventStoreName;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _jsonSerializerOptions = jsonSerializerOptions;
        Name = eventStoreName;
        Namespace = @namespace;
        Connection = connection;
        _servicesAccessor = (connection as IChronicleServicesAccessor)!;
        _correlationIdAccessor = correlationIdAccessor;
        _concurrencyScopeStrategies = concurrencyScopeStrategies;
        EventTypes = new EventTypes(this, schemaGenerator, clientArtifactsProvider);
        UnitOfWorkManager = new UnitOfWorkManager(this);
        _correlationIdAccessor = correlationIdAccessor;

        _eventSerializer = new EventSerializer(
            clientArtifactsProvider,
            serviceProvider,
            EventTypes,
            jsonSerializerOptions);

        Constraints = new Constraints(
            this,
            [
                new ConstraintsByBuilderProvider(clientArtifactsProvider, EventTypes, namingPolicy, serviceProvider),
                new UniqueConstraintProvider(clientArtifactsProvider, EventTypes, namingPolicy),
                new UniqueEventTypeConstraintsProvider(clientArtifactsProvider, EventTypes)
            ]);

        EventLog = new EventLog(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            Constraints,
            _eventSerializer,
            correlationIdAccessor,
            concurrencyScopeStrategies,
            causationManager,
            UnitOfWorkManager,
            identityProvider,
            jsonSerializerOptions);

        Jobs = new Jobs.Jobs(this);

        Reactors = new Reactors.Reactors(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            new ReactorMiddlewares(clientArtifactsProvider, serviceProvider),
            _eventSerializer,
            causationManager,
            identityProvider,
            loggerFactory.CreateLogger<Reactors.Reactors>(),
            loggerFactory);

        var reducerObservers = new ReducerObservers();

        Reducers = new Reducers.Reducers(
            this,
            clientArtifactsProvider,
            serviceProvider,
            new ReducerValidator(),
            EventTypes,
            namingPolicy,
            jsonSerializerOptions,
            identityProvider,
            reducerObservers,
            loggerFactory.CreateLogger<Reducers.Reducers>());

        var projections = new Projections.Projections(
            this,
            EventTypes,
            clientArtifactsProvider,
            namingPolicy,
            serviceProvider,
            jsonSerializerOptions);

        Projections = projections;
        Webhooks = new Webhooks.Webhooks(EventTypes, this, loggerFactory.CreateLogger<Webhooks.Webhooks>());
        FailedPartitions = new FailedPartitions(this);

        var readModelsWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, jsonSerializerOptions));

        ReadModels = new ReadModels.ReadModels(
            this,
            namingPolicy,
            projections,
            Reducers,
            schemaGenerator,
            jsonSerializerOptions,
            readModelsWatcherManager,
            reducerObservers);

        Seeding = new EventSeeding(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            _eventSerializer,
            clientArtifactsProvider,
            serviceProvider);

        if (autoDiscoverAndRegister)
        {
            Connection.Lifecycle.OnConnected += RegisterAll;
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
    public IConstraints Constraints { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IJobs Jobs { get; }

    /// <inheritdoc/>
    public IReactors Reactors { get; }

    /// <inheritdoc/>
    public IReducers Reducers { get; }

    /// <inheritdoc/>
    public IProjections Projections { get; }

    /// <inheritdoc/>
    public IWebhooks Webhooks { get; }

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions { get; }

    /// <inheritdoc/>
    public IReadModels ReadModels { get; }

    /// <inheritdoc/>
    public IEventSeeding Seeding { get; }

    /// <inheritdoc/>
    public async Task DiscoverAll()
    {
        _logger.DiscoverAllArtifacts();

        // We need to discover all event types first, as they are used by the other artifacts
        await EventTypes.Discover();

        await Task.WhenAll(
            Constraints.Discover(),
            Reactors.Discover(),
            Reducers.Discover(),
            Projections.Discover(),
            Seeding.Discover());
    }

    /// <inheritdoc/>
    public async Task RegisterAll()
    {
        _logger.RegisterAllArtifacts();

        // We need to register event types and read models first, as they are used by the other artifacts
        await Task.WhenAll(
            EventTypes.Register(),
            ReadModels.Register());

        await Task.WhenAll(
            Constraints.Register(),
            Reactors.Register(),
            Reducers.Register(),
            Projections.Register(),
            Seeding.Register());
    }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        new EventSequence(
            _eventStoreName,
            Namespace,
            id,
            Connection,
            EventTypes,
            Constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            UnitOfWorkManager,
            _identityProvider,
            _jsonSerializerOptions);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default)
    {
        var namespaces = await _servicesAccessor.Services.Namespaces.GetNamespaces(new() { EventStore = _eventStoreName });
        return namespaces.Select(_ => (EventStoreNamespaceName)_).ToArray();
    }
}
