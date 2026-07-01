// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents the entry point of the fluent builder for appending the event(s) under test during the act phase of an
/// <see cref="EventScenario"/> and returning the resulting <see cref="AppendResult"/>.
/// </summary>
/// <param name="eventLog">The <see cref="IEventLog"/> to append the acted events to.</param>
public class EventScenarioWhenBuilder(IEventLog eventLog)
{
    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> that the acted events belong to.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append events for.</param>
    /// <returns>An <see cref="EventSourceWhenBuilder"/> to continue the fluent chain.</returns>
    public EventSourceWhenBuilder ForEventSource(EventSourceId eventSourceId) =>
        new(eventLog, eventSourceId);
}
