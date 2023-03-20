// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/>.
/// </summary>
public class EventLog : EventSequence, IEventLog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="client"><see cref="IClient"/> for getting connections.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> for working with client observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public EventLog(
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClient client,
        IObserversRegistrar observersRegistrar,
        IExecutionContextManager executionContextManager) : base(
            EventSequenceId.Log,
            eventTypes,
            eventSerializer,
            client,
            observersRegistrar,
            executionContextManager)
    {
    }
}
