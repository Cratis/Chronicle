// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventOutbox"/>.
/// </summary>
public class EventOutbox : EventSequence, IEventOutbox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventOutbox"/> class.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> the sequence is for.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="connection"><see cref="IConnection"/> for getting connections.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventOutbox(
        TenantId tenantId,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IConnection connection,
        IObserversRegistrar observersRegistrar,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IExecutionContextManager executionContextManager) : base(
            tenantId,
            EventSequenceId.Outbox,
            eventTypes,
            eventSerializer,
            connection,
            observersRegistrar,
            causationManager,
            identityProvider,
            executionContextManager)
    {
    }
}
