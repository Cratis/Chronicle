// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Transactions;
using Cratis.Models;
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
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
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
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStore(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        IChronicleConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        ICorrelationIdAccessor correlationIdAccessor,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameConvention modelNameConvention,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventStore>();
        _eventStoreName = eventStoreName;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        Name = eventStoreName;
        Namespace = @namespace;
        Connection = connection;
        _servicesAccessor = (connection as IChronicleServicesAccessor)!;
        _correlationIdAccessor = correlationIdAccessor;
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
                new ConstraintsByBuilderProvider(clientArtifactsProvider, EventTypes, serviceProvider),
                new UniqueConstraintProvider(clientArtifactsProvider, EventTypes),
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
            causationManager,
            identityProvider);

        Reactors = new Reactors.Reactors(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            new ReactorMiddlewares(clientArtifactsProvider, serviceProvider),
            _eventSerializer,
            causationManager,
            loggerFactory.CreateLogger<Reactors.Reactors>(),
            loggerFactory);

        var modelNameResolver = new ModelNameResolver(modelNameConvention);

        Reducers = new Reducers.Reducers(
            this,
            clientArtifactsProvider,
            serviceProvider,
            new ReducerValidator(),
            EventTypes,
            _eventSerializer,
            modelNameResolver,
            schemaGenerator,
            jsonSerializerOptions,
            loggerFactory.CreateLogger<Reducers.Reducers>());

        var projections = new Projections.Projections(
            this,
            EventTypes,
            clientArtifactsProvider,
            schemaGenerator,
            modelNameResolver,
            _eventSerializer,
            serviceProvider,
            jsonSerializerOptions);
        projections.SetRulesProjections(new RulesProjections(serviceProvider, clientArtifactsProvider, EventTypes, modelNameResolver, schemaGenerator, jsonSerializerOptions));
        Projections = projections;

        AggregateRootFactory = new AggregateRootFactory(
            this,
            new AggregateRootMutatorFactory(
                this,
                new AggregateRootStateProviders(Reducers, Projections, serviceProvider),
                new AggregateRootEventHandlersFactory(EventTypes),
                _eventSerializer,
                correlationIdAccessor),
            UnitOfWorkManager,
            serviceProvider);
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
    public IAggregateRootFactory AggregateRootFactory { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes { get; }

    /// <inheritdoc/>
    public IConstraints Constraints { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IReactors Reactors { get; }

    /// <inheritdoc/>
    public IReducers Reducers { get; }

    /// <inheritdoc/>
    public IProjections Projections { get; }

    /// <inheritdoc/>
    public async Task DiscoverAll()
    {
        _logger.DiscoverAllArtifacts();

        // We need to discover all event types first, as they are used by the other artifacts
        await EventTypes.Discover();
        await Constraints.Discover();

        await Task.WhenAll(
            Reactors.Discover(),
            Reducers.Discover(),
            Projections.Discover());
    }

    /// <inheritdoc/>
    public async Task RegisterAll()
    {
        _logger.RegisterAllArtifacts();

        // We need to register event types first, as they are used by the other artifacts
        await EventTypes.Register();
        await Constraints.Register();

        await Task.WhenAll(
            Reactors.Register(),
            Reducers.Register(),
            Projections.Register());
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
            _causationManager,
            _identityProvider);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default)
    {
        var namespaces = await _servicesAccessor.Services.Namespaces.GetNamespaces(new() { EventStore = _eventStoreName });
        return namespaces.Select(_ => (EventStoreNamespaceName)_).ToArray();
    }
}
