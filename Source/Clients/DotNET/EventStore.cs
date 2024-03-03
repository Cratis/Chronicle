// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;
using Cratis.Models;
using Cratis.Observation;
using Cratis.Projections;
using Cratis.Reducers;
using Cratis.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis;

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
    /// <param name="connection"><see cref="ICratisConnection"/> for working with the connection to Cratis Kernel.</param>
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
        ICratisConnection connection,
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

        EventOutbox = new EventOutbox(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            _eventSerializer,
            causationManager,
            identityProvider);

        Observers = new Observers(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            new ObserverMiddlewares(clientArtifactsProvider, serviceProvider),
            _eventSerializer,
            causationManager,
            loggerFactory.CreateLogger<Observers>(),
            loggerFactory);

        Reducers = new Reducers.Reducers(this, clientArtifactsProvider);
        Projections = new Projections.Projections(
            this,
            clientArtifactsProvider,
            schemaGenerator,
            new ModelNameResolver(modelNameConvention),
            serviceProvider,
            jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public EventStoreName EventStoreName { get; }

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace { get; }

    /// <inheritdoc/>
    public ICratisConnection Connection { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox EventOutbox { get; }

    /// <inheritdoc/>
    public IObservers Observers { get; }

    /// <inheritdoc/>
    public IReducers Reducers { get; }

    /// <inheritdoc/>
    public IProjections Projections { get; }

    /// <inheritdoc/>
    public Task DiscoverAll()
    {
        _logger.DiscoverAllArtifacts();
        return Task.WhenAll(
            EventTypes.Discover(),
            Observers.Discover(),
            Reducers.Discover(),
            Projections.Discover());
    }

    /// <inheritdoc/>
    public Task RegisterAll()
    {
        _logger.RegisterAllArtifacts();
        return Task.WhenAll(
            EventTypes.Register(),
            Observers.Register());
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
