// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Client;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
public class AggregateRootFactory : IAggregateRootFactory
{
    readonly IDictionary<Type, IAggregateRootEventHandlers> _eventHandlersByAggregateRootType = new Dictionary<Type, IAggregateRootEventHandlers>();
    readonly IAggregateRootStateProvider _aggregateRootStateManager;
    readonly IAggregateRootEventHandlersFactory _aggregateRootEventHandlersFactory;
    readonly ICausationManager _causationManager;
    readonly IEventSequences _eventSequences;
    readonly IEventSerializer _eventSerializer;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
    /// </summary>
    /// <param name="aggregateRootStateManager"><see cref="IAggregateRootStateProvider"/> for managing state for an aggregate root.</param>
    /// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for creating <see cref="IAggregateRootEventHandlers"/>.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
    /// <param name="eventSequences"><see cref="IEventSequences"/> to get event sequence to work with.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
    public AggregateRootFactory(
        IAggregateRootStateProvider aggregateRootStateManager,
        IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
        ICausationManager causationManager,
        IEventSequences eventSequences,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider)
    {
        _aggregateRootStateManager = aggregateRootStateManager;
        _aggregateRootEventHandlersFactory = aggregateRootEventHandlersFactory;
        _causationManager = causationManager;
        _eventSequences = eventSequences;
        _eventSerializer = eventSerializer;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<T> Get<T>(EventSourceId id)
        where T : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<T>(_serviceProvider);
        var eventHandlers = GetEventHandlers<T>();

        var eventSequence = _eventSequences.GetEventSequence(aggregateRoot.EventSequenceId);
        var events = Enumerable.Empty<AppendedEvent>();

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            SetAggregateRootInternals(id, eventHandlers, eventSequence, knownAggregateRoot);

            if (aggregateRoot.IsStateful)
            {
                events = await _aggregateRootStateManager.Provide(knownAggregateRoot, eventSequence);
            }
        }

        await HandleAnyEventHandlers(aggregateRoot, id, eventHandlers, eventSequence, events);

        return aggregateRoot;
    }

    void SetAggregateRootInternals(EventSourceId id, IAggregateRootEventHandlers eventHandlers, IEventSequence eventSequence, AggregateRoot aggregateRoot)
    {
        aggregateRoot.EventSequence = eventSequence;
        aggregateRoot.CausationManager = _causationManager;
        aggregateRoot.EventHandlers = eventHandlers;
        aggregateRoot._eventSourceId = id;
    }

    async Task<IImmutableList<AppendedEvent>> GetEvents<T>(EventSourceId id, T aggregateRoot, IAggregateRootEventHandlers eventHandlers, IEventSequence eventSequence)
        where T : IAggregateRoot
    {
        IImmutableList<AppendedEvent> events = ImmutableList<AppendedEvent>.Empty;
        if (aggregateRoot.IsStateful || eventHandlers.HasHandleMethods)
        {
            events = await eventSequence.GetForEventSourceIdAndEventTypes(id, eventHandlers.EventTypes);
        }

        return events;
    }

    IAggregateRootEventHandlers GetEventHandlers<T>()
        where T : IAggregateRoot
    {
        if (!_eventHandlersByAggregateRootType.TryGetValue(typeof(T), out var eventHandlers))
        {
            eventHandlers = _aggregateRootEventHandlersFactory.Create(typeof(T));
            _eventHandlersByAggregateRootType[typeof(T)] = eventHandlers;
        }

        return eventHandlers;
    }

    async Task HandleAnyEventHandlers<T>(T aggregateRoot, EventSourceId id, IAggregateRootEventHandlers eventHandlers, IEventSequence eventSequence, IEnumerable<AppendedEvent> events)
        where T : IAggregateRoot
    {
        if (eventHandlers.HasHandleMethods)
        {
            if (!events.Any())
            {
                events = await GetEvents(id, aggregateRoot, eventHandlers, eventSequence);
            }

            var deserializedEventsTasks = events.Select(async _ =>
            {
                var @event = await _eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);
            await eventHandlers.Handle(aggregateRoot, deserializedEvents);
        }
    }
}
