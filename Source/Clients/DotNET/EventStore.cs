// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Models;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IEventSerializer _eventSerializer;
    readonly ILogger<EventStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store.</param>
    /// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Cratis Kernel.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
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
        EventStoreName = eventStoreName;
        Namespace = @namespace;
        Connection = connection;
        EventTypes = new Events.EventTypes(this, schemaGenerator, clientArtifactsProvider);

        _eventSerializer = new EventSerializer(
            clientArtifactsProvider,
            serviceProvider,
            EventTypes,
            jsonSerializerOptions);

        EventLog = new EventLog(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            _eventSerializer,
            causationManager,
            identityProvider);

        Reactions = new Reactions.Reactions(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            new ReactionMiddlewares(clientArtifactsProvider, serviceProvider),
            _eventSerializer,
            causationManager,
            loggerFactory.CreateLogger<Reactions.Reactions>(),
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
            loggerFactory.CreateLogger<Reducers.Reducers>());

        Projections = new Projections.Projections(
            this,
            EventTypes,
            clientArtifactsProvider,
            schemaGenerator,
            modelNameResolver,
            _eventSerializer,
            serviceProvider,
            jsonSerializerOptions);

        AggregateRootFactory = new AggregateRootFactory(
            this,
            new AggregateRootStateProviders(Reducers, Projections),
            new AggregateRootEventHandlersFactory(EventTypes),
            causationManager,
            _eventSerializer,
            serviceProvider);
    }

    /// <inheritdoc/>
    public EventStoreName EventStoreName { get; }

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace { get; }

    /// <inheritdoc/>
    public IChronicleConnection Connection { get; }

    /// <inheritdoc/>
    public IAggregateRootFactory AggregateRootFactory { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IReactions Reactions { get; }

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

        await Task.WhenAll(
            Reactions.Discover(),
            Reducers.Discover(),
            Projections.Discover());
    }

    /// <inheritdoc/>
    public async Task RegisterAll()
    {
        _logger.RegisterAllArtifacts();

        // We need to register event types first, as they are used by the other artifacts
        await EventTypes.Register();

        await Task.WhenAll(
            Reactions.Register(),
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
            _eventSerializer,
            _causationManager,
            _identityProvider);
}
