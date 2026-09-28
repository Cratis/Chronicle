// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorBuilder"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/> event types are resolved from.</param>
internal sealed class ReactorBuilder(IEventTypes eventTypes) : IReactorBuilder
{
    readonly List<(EventType EventType, Type ClrType, Func<object, EventContext, Task> Handler)> _typedHandlers = [];
    readonly List<Func<object, EventContext, Task>> _catchAllHandlers = [];
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    bool _isReplayable = true;

    /// <inheritdoc/>
    public IReactorBuilder On<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AddTyped<TEvent>((@event, _) =>
        {
            handler((TEvent)@event);
            return Task.CompletedTask;
        });
    }

    /// <inheritdoc/>
    public IReactorBuilder On<TEvent>(Action<TEvent, EventContext> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AddTyped<TEvent>((@event, context) =>
        {
            handler((TEvent)@event, context);
            return Task.CompletedTask;
        });
    }

    /// <inheritdoc/>
    public IReactorBuilder On<TEvent>(Func<TEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AddTyped<TEvent>((@event, _) => handler((TEvent)@event));
    }

    /// <inheritdoc/>
    public IReactorBuilder On<TEvent>(Func<TEvent, EventContext, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AddTyped<TEvent>((@event, context) => handler((TEvent)@event, context));
    }

    /// <inheritdoc/>
    public IReactorBuilder Subscribe(Action<object, EventContext> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _catchAllHandlers.Add((@event, context) =>
        {
            handler(@event, context);
            return Task.CompletedTask;
        });
        return this;
    }

    /// <inheritdoc/>
    public IReactorBuilder Subscribe(Func<object, EventContext, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _catchAllHandlers.Add(handler);
        return this;
    }

    /// <inheritdoc/>
    public IReactorBuilder OnEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    /// <inheritdoc/>
    public IReactorBuilder NotReplayable()
    {
        _isReplayable = false;
        return this;
    }

    /// <summary>
    /// Build the definition of what has been declared.
    /// </summary>
    /// <param name="id">The identifier of the reactor.</param>
    /// <returns>The <see cref="IReactorDefinition"/>.</returns>
    /// <exception cref="NoEventTypesForReactor">Thrown when the definition subscribes to no event types.</exception>
    internal IReactorDefinition Build(ReactorId id)
    {
        var subscriptions = _typedHandlers
            .DistinctBy(_ => _.EventType.Id)
            .Select(_ => (_.EventType, _.ClrType))
            .ToList();

        var subscribesToAllEvents = _catchAllHandlers.Count > 0;
        if (subscribesToAllEvents)
        {
            // The kernel is asked for an explicit list of event types rather than for everything, because a catch-all
            // hands the handler an object, and only an event type this client knows can become one. Each is taken in
            // its latest generation - the one the client appends and deserializes to by default.
            var latestGenerations = eventTypes.All
                .Where(eventType => !subscriptions.Exists(_ => _.EventType.Id == eventType.Id))
                .GroupBy(eventType => eventType.Id)
                .Select(generations => generations.MaxBy(eventType => eventType.Generation.Value)!)
                .OrderBy(eventType => eventType.Id.Value, StringComparer.Ordinal);

            foreach (var eventType in latestGenerations)
            {
                subscriptions.Add((eventType, eventTypes.GetClrTypeFor(eventType.Id, eventType.Generation)));
            }
        }

        if (subscriptions.Count == 0)
        {
            throw new NoEventTypesForReactor(id);
        }

        var handlersByType = _typedHandlers
            .GroupBy(_ => _.ClrType)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Func<object, EventContext, Task>>)[.. group.Select(_ => _.Handler)]);

        return new ReactorDelegateDefinition(
            id,
            _eventSequenceId,
            _isReplayable,
            subscribesToAllEvents,
            [.. subscriptions.Select(_ => _.EventType)],
            [.. subscriptions.Select(_ => _.ClrType)],
            handlersByType,
            [.. _catchAllHandlers]);
    }

    ReactorBuilder AddTyped<TEvent>(Func<object, EventContext, Task> handler)
    {
        var clrType = typeof(TEvent);
        _typedHandlers.Add((eventTypes.GetEventTypeFor(clrType), clrType, handler));
        return this;
    }
}
