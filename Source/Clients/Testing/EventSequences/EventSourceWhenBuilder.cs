// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a builder for appending the event(s) under test for a specific <see cref="EventSourceId"/> during the act
/// phase of an <see cref="EventScenario"/>, returning the resulting <see cref="AppendResult"/>.
/// </summary>
/// <param name="eventLog">The <see cref="IEventLog"/> to append the acted events to.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate the acted events with.</param>
public class EventSourceWhenBuilder(IEventLog eventLog, EventSourceId eventSourceId)
{
    /// <summary>
    /// Appends the event(s) under test for the current <see cref="EventSourceId"/> through the real kernel
    /// <c>EventSequence</c> grain and returns the resulting <see cref="AppendResult"/>.
    /// </summary>
    /// <param name="event">The event under test — the action being exercised.</param>
    /// <param name="additionalEvents">Any further events that make up the same action, appended in order.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> resolving to the <see cref="AppendResult"/> of the append. When more than one event
    /// is supplied, each is appended in order and the result of the final append is returned.
    /// </returns>
    public async Task<AppendResult> Events(object @event, params object[] additionalEvents)
    {
        var result = await eventLog.Append(eventSourceId, @event);
        foreach (var additionalEvent in additionalEvents)
        {
            result = await eventLog.Append(eventSourceId, additionalEvent);
        }

        return result;
    }
}
