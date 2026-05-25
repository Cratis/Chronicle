// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of event objects returned from a reactor handler method.
/// Each event is appended to the event log using the <see cref="EventSourceId"/> from the triggering <see cref="EventContext"/>.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for checking whether the values are known event types.</param>
public class EventsResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(EventContext eventContext, object value) =>
        value is IEnumerable<object> events &&
        events.All(e => eventTypes.HasFor(e.GetType()));

    /// <inheritdoc/>
    public async Task Handle(EventContext eventContext, IEventStore eventStore, object value)
    {
        foreach (var @event in (IEnumerable<object>)value)
        {
            await eventStore.EventLog.Append(eventContext.EventSourceId, @event);
        }
    }
}
