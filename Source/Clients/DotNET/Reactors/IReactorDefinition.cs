// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a reactor declared fluently through an <see cref="IReactorBuilder"/>, which can be inspected before and
/// after it is registered.
/// </summary>
public interface IReactorDefinition
{
    /// <summary>
    /// Gets the identifier of the reactor.
    /// </summary>
    ReactorId Id { get; }

    /// <summary>
    /// Gets the event sequence the reactor observes.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets a value indicating whether the reactor can be replayed.
    /// </summary>
    bool IsReplayable { get; }

    /// <summary>
    /// Gets a value indicating whether the reactor subscribes to all events, through
    /// <see cref="IReactorBuilder.Subscribe(Action{object, EventContext})"/> or its asynchronous equivalent.
    /// </summary>
    bool SubscribesToAllEvents { get; }

    /// <summary>
    /// Gets the event types the reactor subscribes to - exactly the set Chronicle is asked to deliver.
    /// </summary>
    /// <remarks>
    /// For a reactor with only typed handlers this is one event type per handled type. For an all-events subscription
    /// it is every event type the client knew when the reactor was defined, in its latest generation unless a typed
    /// handler names a different one.
    /// </remarks>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Gets the CLR types of the events the reactor subscribes to, one for each of the <see cref="EventTypes"/>.
    /// </summary>
    IEnumerable<Type> ClrTypes { get; }

    /// <summary>
    /// Dispatch an event to the handlers that apply to it, the way the reactor does for every delivered event.
    /// </summary>
    /// <param name="event">The event to dispatch.</param>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>Awaitable task, completing when every applicable handler has completed.</returns>
    /// <remarks>
    /// Typed handlers apply when the event's type is exactly the type they were declared for; catch-all handlers apply
    /// to every event. An event no handler applies to is ignored.
    /// </remarks>
    Task Handle(object @event, EventContext context);
}
