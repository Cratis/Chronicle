// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a builder for seeding events for a specific <see cref="EventSourceId"/> into an <see cref="EventScenario"/>.
/// </summary>
/// <param name="eventLog">The <see cref="InMemoryEventLog"/> to seed events into.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for mapping CLR types.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the seeded events with.</param>
public class EventSourceGivenBuilder(InMemoryEventLog eventLog, IEventTypes eventTypes, EventSourceId eventSourceId)
{
    /// <summary>
    /// Seeds the provided event instances into the event log for the current <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="events">The event instances to seed, in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Events(params object[] events)
    {
        var baseSequenceNumber = (await eventLog.GetNextSequenceNumber()).Value;

        var seededEvents = events.Select((@event, index) =>
        {
            var eventType = eventTypes.GetEventTypeFor(@event.GetType());
            return new AppendedEvent(
                EventContext.From(
                    EventStoreName.NotSet,
                    EventStoreNamespaceName.NotSet,
                    eventType,
                    EventSourceType.Default,
                    eventSourceId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    (EventSequenceNumber)(baseSequenceNumber + (ulong)index),
                    CorrelationId.New()),
                @event.AsExpandoObject(true));
        });

        eventLog.Seed(seededEvents);
    }
}
