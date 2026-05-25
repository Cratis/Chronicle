// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single event object returned from a reactor handler method.
/// The event is appended to the event log using the <see cref="EventSourceId"/> from the triggering <see cref="EventContext"/>.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for checking whether the value is a known event type.</param>
public class EventResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(EventContext eventContext, object value) =>
        eventTypes.HasFor(value.GetType());

    /// <inheritdoc/>
    public Task Handle(EventContext eventContext, IEventStore eventStore, object value) =>
        eventStore.EventLog.Append(eventContext.EventSourceId, value);
}
