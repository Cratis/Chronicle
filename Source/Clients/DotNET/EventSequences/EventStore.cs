// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventOutbox Outbox { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> in the system.</param>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for connecting to kernel.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with execution context.</param>
    public EventStore(
        IEventTypes eventTypes,
        IEventSerializer serializer,
        IClient client,
        IObserversRegistrar observersRegistrar,
        IExecutionContextManager executionContextManager)
    {
        EventLog = new EventLog(
            eventTypes,
            serializer,
            client,
            observersRegistrar,
            executionContextManager);

        Outbox = new EventOutbox(
            eventTypes,
            serializer,
            client,
            observersRegistrar,
            executionContextManager);
    }
}
