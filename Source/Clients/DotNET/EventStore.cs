// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly TenantId _tenantId;
    readonly ICratisConnection _connection;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="tenantId">Tenant identifier for the event store.</param>
    /// <param name="connection"><see cref="ICratisConnection"/> for working with the connection to Cratis Kernel.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    public EventStore(
        EventStoreName eventStoreName,
        TenantId tenantId,
        ICratisConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        IIdentityProvider identityProvider)
    {
        EventLog = new EventLog(
            eventStoreName,
            tenantId,
            connection,
            eventTypes,
            eventSerializer,
            causationManager,
            identityProvider);

        EventOutbox = new EventOutbox(
            eventStoreName,
            tenantId,
            connection,
            eventTypes,
            eventSerializer,
            causationManager,
            identityProvider);

        Observers = new Observers(clientArtifactsProvider, eventTypes);

        _eventStoreName = eventStoreName;
        _tenantId = tenantId;
        _connection = connection;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
    }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox EventOutbox { get; }

    /// <inheritdoc/>
    public IObservers Observers { get; }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        new EventSequence(
            _eventStoreName,
            _tenantId,
            id,
            _connection,
            _eventTypes,
            _eventSerializer,
            _causationManager,
            _identityProvider);
}
