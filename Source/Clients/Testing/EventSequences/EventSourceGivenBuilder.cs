// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a builder for seeding events for a specific <see cref="EventSourceId"/> into an <see cref="EventScenario"/>.
/// </summary>
/// <param name="eventLog">The <see cref="IEventLog"/> to seed events into.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the seeded events with.</param>
public class EventSourceGivenBuilder(IEventLog eventLog, EventSourceId eventSourceId)
{
    /// <summary>
    /// Seeds the provided event instances into the event log for the current <see cref="EventSourceId"/>
    /// by appending them through the real kernel <c>EventSequence</c> grain.
    /// </summary>
    /// <param name="events">The event instances to seed, in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Events(params object[] events)
    {
        foreach (var @event in events)
        {
            await eventLog.Append(eventSourceId, @event);
        }
    }
}
