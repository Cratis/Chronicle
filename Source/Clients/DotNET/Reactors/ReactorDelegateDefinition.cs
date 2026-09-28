// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorDefinition"/> dispatching to handler delegates.
/// </summary>
/// <param name="id">The identifier of the reactor.</param>
/// <param name="eventSequenceId">The event sequence the reactor observes.</param>
/// <param name="isReplayable">Whether the reactor can be replayed.</param>
/// <param name="subscribesToAllEvents">Whether the reactor subscribes to all events.</param>
/// <param name="eventTypes">The event types subscribed to.</param>
/// <param name="clrTypes">The CLR types of the event types subscribed to.</param>
/// <param name="handlersByType">Typed handlers, by the exact CLR type they handle.</param>
/// <param name="catchAllHandlers">Handlers for every event.</param>
internal sealed class ReactorDelegateDefinition(
    ReactorId id,
    EventSequenceId eventSequenceId,
    bool isReplayable,
    bool subscribesToAllEvents,
    IReadOnlyList<EventType> eventTypes,
    IReadOnlyList<Type> clrTypes,
    IReadOnlyDictionary<Type, IReadOnlyList<Func<object, EventContext, Task>>> handlersByType,
    IReadOnlyList<Func<object, EventContext, Task>> catchAllHandlers) : IReactorDefinition
{
    /// <inheritdoc/>
    public ReactorId Id => id;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId => eventSequenceId;

    /// <inheritdoc/>
    public bool IsReplayable => isReplayable;

    /// <inheritdoc/>
    public bool SubscribesToAllEvents => subscribesToAllEvents;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => eventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> ClrTypes => clrTypes;

    /// <inheritdoc/>
    public async Task Handle(object @event, EventContext context)
    {
        if (handlersByType.TryGetValue(@event.GetType(), out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(@event, context);
            }
        }

        foreach (var handler in catchAllHandlers)
        {
            await handler(@event, context);
        }
    }
}
