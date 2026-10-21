// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Allocates scenario-local occurrences for history and live givens without appending or delivering events.
/// </summary>
/// <param name="eventTypes">The event types available to the scenario.</param>
/// <param name="eventStore">The event store name included in the context.</param>
/// <param name="namespace">The namespace included in the context.</param>
internal sealed class ScenarioOccurrenceRecorder(IEventTypes eventTypes, EventStoreName eventStore, EventStoreNamespaceName @namespace)
{
    EventSequenceNumber _nextSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Records occurrences in order, allocating a context only as each event is enumerated.
    /// </summary>
    /// <param name="eventSourceId">The source of the events.</param>
    /// <param name="events">The events to record.</param>
    /// <param name="eventSourceType">The source type.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <returns>The recorded occurrences.</returns>
    internal IEnumerable<RecordedOccurrence> Record(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId)
    {
        foreach (var @event in events)
        {
            var context = EventContext.From(
                eventStore,
                @namespace,
                eventTypes.GetEventTypeFor(@event.GetType()),
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                _nextSequenceNumber,
                CorrelationId.New());
            _nextSequenceNumber += 1;
            yield return new(@event, context);
        }
    }
}
