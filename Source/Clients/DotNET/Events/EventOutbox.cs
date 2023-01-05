// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventOutbox"/>.
/// </summary>
public class EventOutbox : EventSequence, IEventOutbox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventOutbox"/> class.
    /// </summary>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for getting connections.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventOutbox(
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClient client,
        IExecutionContextManager executionContextManager) : base(
            EventSequenceId.Outbox,
            eventTypes,
            eventSerializer,
            client,
            executionContextManager)
    {
    }
}
