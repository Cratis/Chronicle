// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/>.
/// </summary>
public class EventSequences : IEventSequences
{
    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox Outbox { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequences"/> class.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> the sequence is for.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for getting connections.</param>
    /// <param name="observerRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventSequences(
        TenantId tenantId,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClient client,
        IObserversRegistrar observerRegistrar,
        IExecutionContextManager executionContextManager)
    {
        EventLog = new EventLog(tenantId, eventTypes, eventSerializer, client, observerRegistrar, executionContextManager);
        Outbox = new EventOutbox(tenantId, eventTypes, eventSerializer, client, observerRegistrar, executionContextManager);
    }
}
