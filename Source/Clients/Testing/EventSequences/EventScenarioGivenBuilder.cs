// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents the entry point of the fluent builder for seeding pre-existing events into an <see cref="EventScenario"/>.
/// </summary>
/// <param name="eventLog">The <see cref="InMemoryEventLog"/> to seed events into.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for mapping CLR types.</param>
public class EventScenarioGivenBuilder(InMemoryEventLog eventLog, IEventTypes eventTypes)
{
    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> that the events belong to.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to seed events for.</param>
    /// <returns>An <see cref="EventSourceGivenBuilder"/> to continue the fluent chain.</returns>
    public EventSourceGivenBuilder ForEventSource(EventSourceId eventSourceId) =>
        new(eventLog, eventTypes, eventSourceId);
}
