// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a builder for declaring a reactor fluently, with handlers for the events it reacts to.
/// </summary>
/// <remarks>
/// <para>
/// Handlers are awaited, and an event is only acknowledged once every handler for it has completed. A handler that
/// throws or returns a failed task fails the event's partition, exactly as a failing method on a discovered reactor does.
/// </para>
/// <para>
/// When an event has typed handlers and the reactor also has catch-all handlers, the typed handlers run first, in the
/// order they were declared, followed by the catch-all handlers in the order they were declared.
/// </para>
/// </remarks>
public interface IReactorBuilder
{
    /// <summary>
    /// Handle a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle. Subscribes the reactor to this event type only.</typeparam>
    /// <param name="handler">The handler receiving the event.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="TypeIsNotAnEventType">Thrown when <typeparamref name="TEvent"/> is not a known event type.</exception>
    IReactorBuilder On<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Handle a specific event type, with its <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle. Subscribes the reactor to this event type only.</typeparam>
    /// <param name="handler">The handler receiving the event and its context.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="TypeIsNotAnEventType">Thrown when <typeparamref name="TEvent"/> is not a known event type.</exception>
    IReactorBuilder On<TEvent>(Action<TEvent, EventContext> handler);

    /// <summary>
    /// Handle a specific event type asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle. Subscribes the reactor to this event type only.</typeparam>
    /// <param name="handler">The handler receiving the event.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="TypeIsNotAnEventType">Thrown when <typeparamref name="TEvent"/> is not a known event type.</exception>
    IReactorBuilder On<TEvent>(Func<TEvent, Task> handler);

    /// <summary>
    /// Handle a specific event type asynchronously, with its <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle. Subscribes the reactor to this event type only.</typeparam>
    /// <param name="handler">The handler receiving the event and its context.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="TypeIsNotAnEventType">Thrown when <typeparamref name="TEvent"/> is not a known event type.</exception>
    IReactorBuilder On<TEvent>(Func<TEvent, EventContext, Task> handler);

    /// <summary>
    /// Handle every event, whatever its type. This makes the reactor an all-events subscription.
    /// </summary>
    /// <param name="handler">The handler receiving each event and its context.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// The subscription covers every event type this client knows when the reactor is defined - the event types
    /// discovered in the client's artifacts. An event type the client does not know cannot be turned into an object to
    /// hand to the handler, so it is not part of the subscription; nor is an event type that becomes known only after
    /// the reactor was defined. <see cref="IReactorDefinition.EventTypes"/> tells exactly which event types that is.
    /// </remarks>
    IReactorBuilder Subscribe(Action<object, EventContext> handler);

    /// <summary>
    /// Handle every event asynchronously, whatever its type. This makes the reactor an all-events subscription.
    /// </summary>
    /// <param name="handler">The handler receiving each event and its context.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// The subscription covers the same event types as <see cref="Subscribe(Action{object, EventContext})"/> describes.
    /// </remarks>
    IReactorBuilder Subscribe(Func<object, EventContext, Task> handler);

    /// <summary>
    /// Selects the event sequence to observe. Defaults to the event log.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <returns>The builder for continuation.</returns>
    IReactorBuilder OnEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Disables replay for the reactor, for handlers whose side effects must not be repeated.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IReactorBuilder NotReplayable();
}
