// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
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
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for getting connections.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventOutbox(
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClient client,
        IObserversRegistrar observersRegistrar,
        IExecutionContextManager executionContextManager) : base(
            EventSequenceId.Outbox,
            eventTypes,
            eventSerializer,
            client,
            observersRegistrar,
            executionContextManager)
    {
    }
}
