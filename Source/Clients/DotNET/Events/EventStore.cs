// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    /// <inheritdoc/>
    public IEventSequence EventLog { get; }

    /// <inheritdoc/>
    public IEventSequence Outbox { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> in the system.</param>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="connectionFactory"><see cref="IConnectionFactory"/> for connecting to kernel.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with execution context.</param>
    public EventStore(
        IEventTypes eventTypes,
        IEventSerializer serializer,
        IConnectionFactory connectionFactory,
        IExecutionContextManager executionContextManager)
    {
        EventLog = new EventSequence(
            Store.EventSequenceId.Log,
            eventTypes,
            serializer,
            connectionFactory,
            executionContextManager);

        Outbox = new EventSequence(
            Store.EventSequenceId.Outbox,
            eventTypes,
            serializer,
            connectionFactory,
            executionContextManager);
    }
}
