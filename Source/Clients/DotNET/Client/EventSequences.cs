// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
public class EventSequences : IEventSequences
{
    readonly TenantId _tenantId;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IConnection _connection;
    readonly IObserversRegistrar _observerRegistrar;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequences"/> class.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> the sequence is for.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="connection"><see cref="IConnection"/> for getting connections.</param>
    /// <param name="observerRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventSequences(
        TenantId tenantId,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IConnection connection,
        IObserversRegistrar observerRegistrar,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IExecutionContextManager executionContextManager)
    {
        EventLog = new EventLog(
            tenantId,
            eventTypes,
            eventSerializer,
            connection,
            observerRegistrar,
            causationManager,
            identityProvider,
            executionContextManager);

        Outbox = new EventOutbox(
            tenantId,
            eventTypes,
            eventSerializer,
            connection,
            observerRegistrar,
            causationManager,
            identityProvider,
            executionContextManager);
        _tenantId = tenantId;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _connection = connection;
        _observerRegistrar = observerRegistrar;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox Outbox { get; }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId eventSequenceId) =>
        new EventSequence(
            _tenantId,
            eventSequenceId,
            _eventTypes,
            _eventSerializer,
            _connection,
            _observerRegistrar,
            _causationManager,
            _identityProvider,
            _executionContextManager);
}
