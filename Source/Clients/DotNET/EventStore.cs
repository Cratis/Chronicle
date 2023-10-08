// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Models;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Reducers;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly TenantId _tenantId;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IEventSerializer _eventSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="tenantId">Tenant identifier for the event store.</param>
    /// <param name="connection"><see cref="ICratisConnection"/> for working with the connection to Cratis Kernel.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public EventStore(
        EventStoreName eventStoreName,
        TenantId tenantId,
        ICratisConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameConvention modelNameConvention,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _eventStoreName = eventStoreName;
        _tenantId = tenantId;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        EventStoreName = eventStoreName;
        TenantId = tenantId;
        Connection = connection;
        EventTypes = new EventTypes(this, schemaGenerator, clientArtifactsProvider);

        _eventSerializer = new EventSerializer(
            clientArtifactsProvider,
            serviceProvider,
            EventTypes,
            jsonSerializerOptions);

        EventLog = new EventLog(
            eventStoreName,
            tenantId,
            connection,
            EventTypes,
            _eventSerializer,
            causationManager,
            identityProvider);

        EventOutbox = new EventOutbox(
            eventStoreName,
            tenantId,
            connection,
            EventTypes,
            _eventSerializer,
            causationManager,
            identityProvider);

        Observers = new Observers(this, clientArtifactsProvider);
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
    public TenantId TenantId { get; }

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
        return Task.WhenAll(
            EventTypes.Discover(),
            Observers.Discover(),
            Reducers.Discover(),
            Projections.Discover());
    }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        new EventSequence(
            _eventStoreName,
            _tenantId,
            id,
            Connection,
            EventTypes,
            _eventSerializer,
            _causationManager,
            _identityProvider);
}
