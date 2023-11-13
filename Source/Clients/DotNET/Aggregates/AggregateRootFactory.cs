// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Client;
using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
public class AggregateRootFactory : IAggregateRootFactory
{
    readonly IDictionary<Type, AggregateRootEventHandlers> _eventHandlersByAggregateRootType = new Dictionary<Type, AggregateRootEventHandlers>();
    readonly IAggregateRootStateManager _aggregateRootStateManager;
    readonly ICausationManager _causationManager;
    readonly IEventTypes _eventTypes;
    readonly IEventSequences _eventSequences;
    readonly IEventSerializer _eventSerializer;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
    /// </summary>
    /// <param name="aggregateRootStateManager"><see cref="IAggregateRootStateManager"/> for managing state for an aggregate root.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="eventSequences"><see cref="IEventSequences"/> to get event sequence to work with.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
    public AggregateRootFactory(
        IAggregateRootStateManager aggregateRootStateManager,
        ICausationManager causationManager,
        IEventTypes eventTypes,
        IEventSequences eventSequences,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider)
    {
        _aggregateRootStateManager = aggregateRootStateManager;
        _causationManager = causationManager;
        _eventTypes = eventTypes;
        _eventSequences = eventSequences;
        _eventSerializer = eventSerializer;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<T> Get<T>(EventSourceId id)
        where T : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<T>(_serviceProvider);

        if (!_eventHandlersByAggregateRootType.TryGetValue(typeof(T), out var eventHandlers))
        {
            eventHandlers = new AggregateRootEventHandlers(typeof(T), _eventTypes);
            _eventHandlersByAggregateRootType[typeof(T)] = eventHandlers;
        }

        var eventSequence = _eventSequences.GetEventSequence(aggregateRoot.EventSequenceId);
        IImmutableList<AppendedEvent> events = ImmutableList<AppendedEvent>.Empty;
        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot.EventSequence = eventSequence;
            knownAggregateRoot.CausationManager = _causationManager;
            knownAggregateRoot.EventHandlers = eventHandlers;
            knownAggregateRoot._eventSourceId = id;

            if (knownAggregateRoot.IsStateful)
            {
                events = await eventSequence.GetForEventSourceIdAndEventTypes(id, eventHandlers.EventTypes);
                await _aggregateRootStateManager.Handle(knownAggregateRoot, events);
            }
        }

        if (!eventHandlers.HasHandleMethods)
        {
            return aggregateRoot;
        }

        if (events.Count == 0)
        {
            events = await eventSequence.GetForEventSourceIdAndEventTypes(id, eventHandlers.EventTypes);
        }

        var deserializedEventsTasks = events.Select(async _ =>
        {
            var @event = await _eventSerializer.Deserialize(_);
            return new EventAndContext(@event, _.Context);
        }).ToArray();

        var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);
        await eventHandlers.Handle(aggregateRoot, deserializedEvents);

        return aggregateRoot;
    }
}
